using BestHTTP;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace Kfc
{

    public class MailBoxV2 : MonoBehaviour
    {
        //系統&私人信件模板
        public GameObject prefab_SystemMail;
        public GameObject prefab_PrivateMail;
        //系統&私人信件拖曳視窗父元件
        public Transform trans_SystemContent;
        public Transform trans_PrivateContent;
        //系統&私人信件父元件
        public GameObject gobj_SystemMail;
        public GameObject gobj_PrivateMail;

        public GameObject mask;

        //網址
        const string HOST = "http://entrance10.mobiusdice.com.tw/demoApi2";
        //const string HOST = "http://localhost:52673";
        string uri_GetMails = HOST + "/GetMailsV2";
        string uri_GetReward = HOST + "/GetRewardV2";
        string uri_GetPartRewards = HOST + "/GetPartRewards";
        string uri_SetMailLockOrNot = HOST + "/SetMailLockOrNotV2";
        

        //必要資訊
        string mAccount;
        string mGuid;

        //外部執行事件
        public Action<string> mMessageBoxEvent;
        public Action<int,string> mGetRewardEvent;

        //目前點下去領取的信件
        GameObject currentMail;
        
        /// <summary>
        /// 開啟信箱
        /// </summary>
        public void OpenMailBox(string _account,string _guid,Action<string> _messageBoxEvent, Action<int, string> _getRewardEvent)
        {
            SlotSoundManager.bSndRef.PlaySoundEffect(ReferenceCenter.Ref.CommonMu.Container, SlotSoundManager.eAudioClip.Snd_ViewOpen.ToString());
            transform.Find("Ani_Box/Gobj_BtnBox/UIBtn_System/Image").gameObject.SetActive(true);
            transform.Find("Ani_Box/Gobj_BtnBox/UIBtn_Private/Image").gameObject.SetActive(false);
            mAccount = _account;
            mGuid = _guid;
            mMessageBoxEvent = _messageBoxEvent;
            mGetRewardEvent = _getRewardEvent;

            Debug.Log(mAccount + "_" + mGuid);
            gameObject.SetActive(true);
            gobj_SystemMail.SetActive(true);

        }

        /// <summary>
        /// 關閉信箱
        /// </summary>
        public void CloseMailBox()
        {
            SlotSoundManager.bSndRef.PlaySoundEffect(ReferenceCenter.Ref.CommonMu.Container, SlotSoundManager.eAudioClip.Snd_ViewClose.ToString());
            mAccount = string.Empty;
            mGuid = string.Empty;
            mMessageBoxEvent = null;

            gameObject.SetActive(false);
        }
        private void OnEnable()
        {
            GetAllMails();
        }

        /// <summary>
        /// 取得所有郵件資訊 - 發送要求
        /// </summary>
        void GetAllMails()
        {
            Uri path = new Uri(uri_GetMails);
            HTTPRequest request = new HTTPRequest(path, HTTPMethods.Post, OnGetAllMailsFinished);
            Debug.Log(mAccount + "_" + mGuid);
            Dictionary<string, object> req = new Dictionary<string, object>();
            req.Add("AccountName", mAccount);
            req.Add("guid", mGuid);

            request.AddHeader("Content-Type", "application/json");
            request.RawData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req));
            request.Send();
            mask.SetActive(true);
        }

        /// <summary>
        /// 取得所有郵件資訊 - 取得回應
        /// </summary>
        private void OnGetAllMailsFinished(HTTPRequest originalRequest, HTTPResponse response)
        {
            mask.SetActive(false);

            if (response == null || response.StatusCode != 200)
            {
                Debug.LogError("與伺服器端連接失敗" + response.StatusCode);
                if (mMessageBoxEvent != null) mMessageBoxEvent("與伺服器端連接失敗");
                return;
            }

            Debug.Log(response.DataAsText);
            //========= 1. 解析所有信件內容，把他轉成List
            JObject jsonResponse = JsonConvert.DeserializeObject<JObject>(response.DataAsText);
            List<Dictionary<string, object>> allMails = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonResponse.GetValue("mails").ToString());
            //========= 2. 開始生成信件(先要判斷是系統信件還是私人信件)
            bool isSystemMail = false;
            foreach (Dictionary<string, object> mailDic in allMails)
            {
                GameObject mail;
                if (mailDic["Sender"] == null || mailDic["Sender"].ToString().Equals("系統") || string.IsNullOrEmpty(mailDic["Sender"].ToString()))
                {
                    mailDic["Sender"] = "系統";
                    mail = Instantiate(prefab_SystemMail, trans_SystemContent);
                    isSystemMail = true;
                }
                else
                {
                    mail = Instantiate(prefab_PrivateMail, trans_PrivateContent);
                    isSystemMail = false;
                }


                //========= 3.將所有的值塞到Bean中
                MailBean bean = mail.GetComponent<MailBean>();
                bean.mailNumber = mailDic["MailNumber"].ToString();

                bean.sender = mailDic["Sender"].ToString();
                bean.senderFbId = (mailDic["SenderFbId"] == null)? string.Empty : mailDic["SenderFbId"].ToString();
                bean.senderNickName = mailDic["SenderNickName"].ToString();
                if (mailDic["SenderIcon"] == null || string.IsNullOrEmpty(mailDic["SenderIcon"].ToString()))
                {
                    bean.senderIcon = -1;
                }
                else
                {
                    bean.senderIcon = Convert.ToInt32(mailDic["SenderIcon"]);
                }
                bean.sendingTime = (DateTime)mailDic["SendingTime"];
                bean.deleteTime = (DateTime)mailDic["DeleteTime"];
                bean.type = Convert.ToInt32(mailDic["Type"]);
                bean.eventName = mailDic["EventName"].ToString();
                bean.reward = mailDic["Reward"].ToString();
                bean.title = mailDic["Title"].ToString();
                bean.title_EN = mailDic["Title_EN"].ToString();
                bean.content = mailDic["Content"].ToString();
                bean.content_EN = mailDic["Content_EN"].ToString();
                bean.isRead = Convert.ToBoolean(mailDic["IsRead"]);
                bean.isHide= Convert.ToBoolean(mailDic["IsHide"]);
                bean.isLock= Convert.ToBoolean(mailDic["IsLock"]);

                //========= 4.填到UI中
                if (isSystemMail)
                {
                    mail.transform.Find("UIScroll_Name/Viewport/Content/UITxt_Name").GetComponent<Text>().text = bean.senderNickName;
                    mail.transform.Find("UITxt_Date").GetComponent<Text>().text = bean.sendingTime.ToString("yyyy.MM.dd");
                    mail.transform.Find("UITxt_Title").GetComponent<Text>().text = bean.title;
                    mail.transform.Find("UIScroll_Content/Viewport/Content/UITxt_Content").GetComponent<Text>().text = bean.content;
                    mail.transform.Find("UIBtn_Lock").gameObject.SetActive(!bean.isLock);
                    mail.transform.Find("UIBtn_Unlock").gameObject.SetActive(bean.isLock);
                    if (bean.type == 0)
                        mail.transform.Find("UIBtn_Collect/Text").GetComponent<Text>().text = "刪除";
                    else
                        mail.transform.Find("UIBtn_Collect/Text").GetComponent<Text>().text = "領取";
                }
                else
                {
                    mail.transform.Find("UIScroll_Name/Viewport/Content/UITxt_Name").GetComponent<Text>().text = bean.senderNickName;
                    mail.transform.Find("UITxt_Date").GetComponent<Text>().text = bean.sendingTime.ToString("yyyy.MM.dd");

                    JObject jobj = null;
                    int money;
                    if (int.TryParse(bean.reward, out money))
                    {
                        //mail.transform.Find("Gobj_Money/UITxt_Money").GetComponent<Text>().text = money.ToString();
                        mail.transform.Find("Gobj_Money/UITxt_Money").GetComponent<Text>().text = string.Format(CultureInfo.InvariantCulture, "{0:#,#}", money);
                    }
                    else
                    {
                        jobj = JsonConvert.DeserializeObject<JObject>(bean.reward);
                        //mail.transform.Find("Gobj_Money/UITxt_Money").GetComponent<Text>().text = jobj.GetValue("money").ToString();
                        mail.transform.Find("Gobj_Money/UITxt_Money").GetComponent<Text>().text = string.Format(CultureInfo.InvariantCulture, "{0:#,#}", (int)jobj.GetValue("money"));

                    }

                    mail.transform.Find("UIBtn_Lock").gameObject.SetActive(!bean.isLock);
                    mail.transform.Find("UIBtn_Unlock").gameObject.SetActive(bean.isLock);
                    if (bean.type == 0)
                        mail.transform.Find("UIBtn_Collect/Text").GetComponent<Text>().text = "刪除";
                    else
                        mail.transform.Find("UIBtn_Collect/Text").GetComponent<Text>().text = "領取";
                }

                mail.SetActive(true);
            }
        }

        /// <summary>
        /// 領取獎勵或刪除郵件 - 發送要求
        /// </summary>
        public void GetMailRewardOrDelete(GameObject _mail)
        {
            SlotSoundManager.bSndRef.PlaySoundEffect(ReferenceCenter.Ref.CommonMu.Container, SlotSoundManager.eAudioClip.Snd_ComClick1.ToString());
            currentMail = _mail;

            MailBean bean = currentMail.GetComponent<MailBean>();
            //先檢查有沒有上鎖，如果有上鎖且type為0，則不允許刪除
            if (bean.type == 0 && bean.isLock == true)
            {
                if (mMessageBoxEvent != null) mMessageBoxEvent("郵件已上鎖");
                return;
            }

            Uri path = new Uri(uri_GetReward);
            HTTPRequest request = new HTTPRequest(path, HTTPMethods.Post, OnGetMailRewardFinished);

            Dictionary<string, object> req = new Dictionary<string, object>();
            req.Add("AccountName", mAccount);
            req.Add("guid", mGuid);
            req.Add("mailNumber", currentMail.GetComponent<MailBean>().mailNumber);

            request.AddHeader("Content-Type", "application/json");
            request.RawData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req));
            request.Send();
            mask.SetActive(true);
        }

        /// <summary>
        /// 領取獎勵或刪除郵件 - 取得回應
        /// </summary>
        void OnGetMailRewardFinished(HTTPRequest originalRequest, HTTPResponse response)
        {
            mask.SetActive(false);

            if (response == null || response.StatusCode != 200)
            {
                Debug.LogError("與伺服器端連接失敗" + response.StatusCode);
                if (mMessageBoxEvent != null) mMessageBoxEvent("與伺服器端連接失敗");
                return;
            }

            Debug.Log(response.DataAsText);
            JObject jsonResponse = JsonConvert.DeserializeObject<JObject>(response.DataAsText);
            if (!jsonResponse.GetValue("result").ToString().Contains("000"))
            {
                Debug.LogError("異常錯誤，請聯絡客服單位");
                if (mMessageBoxEvent != null) mMessageBoxEvent("異常錯誤，請聯絡客服單位");
                return;
            }

            MailBean bean = currentMail.GetComponent<MailBean>();
            switch (bean.type)
            {
                case 0:
                    Destroy(currentMail);
                    currentMail = null;
                    break;
                case 1:
                    bean.type = 0;
                    currentMail.transform.Find("UIBtn_Collect/Text").GetComponent<Text>().text = "刪除";
                    if (mGetRewardEvent != null) mGetRewardEvent(1, jsonResponse.GetValue("playerMoney").ToString());
                    break;
                case 2:
                    bean.type = 0;
                    currentMail.transform.Find("UIBtn_Collect/Text").GetComponent<Text>().text = "刪除";
                    if(mGetRewardEvent != null) mGetRewardEvent(2, jsonResponse.GetValue("playerExp").ToString());
                    break;
                case 3:
                    bean.type = 0;
                    currentMail.transform.Find("UIBtn_Collect/Text").GetComponent<Text>().text = "刪除";
                    if (mGetRewardEvent != null) mGetRewardEvent(3, jsonResponse.GetValue("playerDp").ToString());
                    break;
            }
            
        }

        /// <summary>
        /// 鎖定郵件 - 發送要求
        /// </summary>
        public void SetMailLockOrNot(GameObject _mail)
        {
            SlotSoundManager.bSndRef.PlaySoundEffect(ReferenceCenter.Ref.CommonMu.Container, SlotSoundManager.eAudioClip.Snd_ComClick2.ToString());
            currentMail = _mail;

            Uri path = new Uri(uri_SetMailLockOrNot);
            HTTPRequest request = new HTTPRequest(path, HTTPMethods.Post, OnSetMailLockOrNotFinished);

            Dictionary<string, object> req = new Dictionary<string, object>();
            req.Add("AccountName", mAccount);
            req.Add("guid", mGuid);
            req.Add("mailNumber", currentMail.GetComponent<MailBean>().mailNumber);
            req.Add("isLock", currentMail.transform.Find("UIBtn_Lock").gameObject.activeSelf);

            request.AddHeader("Content-Type", "application/json");
            request.RawData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req));
            request.Send();
            mask.SetActive(true);
        }

        /// <summary>
        /// 鎖定郵件 - 取得回應
        /// </summary>
        void OnSetMailLockOrNotFinished(HTTPRequest originalRequest, HTTPResponse response)
        {
            mask.SetActive(false);

            if (response == null || response.StatusCode != 200)
            {
                Debug.LogError("與伺服器端連接失敗" + response.StatusCode);
                if (mMessageBoxEvent != null) mMessageBoxEvent("與伺服器端連接失敗");
                return;
            }

            Debug.Log(response.DataAsText);
            JObject jsonResponse = JsonConvert.DeserializeObject<JObject>(response.DataAsText);
            if (!jsonResponse.GetValue("result").ToString().Contains("000"))
            {
                Debug.LogError("異常錯誤，請聯絡客服單位");
                if (mMessageBoxEvent != null) mMessageBoxEvent("異常錯誤，請聯絡客服單位");
                return;
            }

            MailBean bean = currentMail.GetComponent<MailBean>();
            bean.isLock = (bool)jsonResponse.GetValue("isLock");
            
            currentMail.transform.Find("UIBtn_Unlock").gameObject.SetActive(bean.isLock);
            currentMail.transform.Find("UIBtn_Lock").gameObject.SetActive(!bean.isLock);
            
        }

        public void GetPartMailsRewadAndDelete()
        {
            SlotSoundManager.bSndRef.PlaySoundEffect(ReferenceCenter.Ref.CommonMu.Container, SlotSoundManager.eAudioClip.Snd_ComClick1.ToString());
            List<string> mailNumbers = null;
            if (gobj_SystemMail.activeSelf)
            {
                if (gobj_SystemMail.transform.childCount == 0)
                {
                    if (mMessageBoxEvent != null) mMessageBoxEvent("無信件可領取");
                    return;
                }

                mailNumbers = gobj_SystemMail.transform.GetComponentsInChildren<MailBean>().Select(tempMail => tempMail.mailNumber).ToList();
            }
            else if(gobj_PrivateMail.activeSelf)
            {
                if (gobj_PrivateMail.transform.childCount == 0)
                {
                    if (mMessageBoxEvent != null) mMessageBoxEvent("無信件可領取");
                    return;
                }

                mailNumbers = gobj_PrivateMail.transform.GetComponentsInChildren<MailBean>().Select(tempMail => tempMail.mailNumber).ToList();
            }

            Uri path = new Uri(uri_GetPartRewards);
            HTTPRequest request = new HTTPRequest(path, HTTPMethods.Post, OnGetPartMailsRewadAndDeleteFinished);

            Dictionary<string, object> req = new Dictionary<string, object>();
            req.Add("AccountName", mAccount);
            req.Add("guid", mGuid);
            req.Add("mailNumbers", mailNumbers);
            
            request.AddHeader("Content-Type", "application/json");
            request.RawData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req));
            request.Send();
            mask.SetActive(true);
        }

        private void OnGetPartMailsRewadAndDeleteFinished(HTTPRequest originalRequest, HTTPResponse response)
        {
            mask.SetActive(false);

            if (response == null || response.StatusCode != 200)
            {
                Debug.LogError("與伺服器端連接失敗" + response.StatusCode);
                if (mMessageBoxEvent != null) mMessageBoxEvent("與伺服器端連接失敗");
                return;
            }

            Debug.Log(response.DataAsText);
            JObject jsonResponse = JsonConvert.DeserializeObject<JObject>(response.DataAsText);
            if (!jsonResponse.GetValue("result").ToString().Contains("000"))
            {
                if (jsonResponse.GetValue("result").ToString().Contains("001"))
                {
                    Debug.LogError("無可領取信件");
                    if (mMessageBoxEvent != null) mMessageBoxEvent("無可領取信件");
                    return;
                }

                Debug.LogError("異常錯誤，請聯絡客服單位");
                if (mMessageBoxEvent != null) mMessageBoxEvent("異常錯誤，請聯絡客服單位");
                return;
            }

            if (mGetRewardEvent != null) mGetRewardEvent((int)jsonResponse.GetValue("playerMoney"), null);
            List<MailBean> beans = null;
            if (gobj_SystemMail.activeSelf)
            {
                if (gobj_SystemMail.transform.childCount == 0)
                {
                    return;
                }

                beans = gobj_SystemMail.transform.GetComponentsInChildren<MailBean>().Select(tempMail => tempMail).ToList();
            }
            else if (gobj_PrivateMail.activeSelf)
            {
                if (gobj_PrivateMail.transform.childCount == 0)
                {
                    return;
                }

                beans = gobj_PrivateMail.transform.GetComponentsInChildren<MailBean>().Select(tempMail => tempMail).ToList();
            }

            
            foreach (MailBean bean in beans)
            {
                if (bean.type == 0)
                {
                    if (bean.isLock == false)
                    {
                        Destroy(bean.gameObject);
                    }
                }
                else
                {
                    bean.type = 0;
                    bean.transform.Find("UIBtn_Collect/Text").GetComponent<Text>().text = "刪除";

                }
            }

        }


        /// <summary>
        /// 開啟系統信件
        /// </summary>
        public void OnSystemMailsBoxClick()
        {
            SlotSoundManager.bSndRef.PlaySoundEffect(ReferenceCenter.Ref.CommonMu.Container, SlotSoundManager.eAudioClip.Snd_ComClick1.ToString());
            transform.Find("Ani_Box/Gobj_BtnBox/UIBtn_System/Image").gameObject.SetActive(true);
            transform.Find("Ani_Box/Gobj_BtnBox/UIBtn_Private/Image").gameObject.SetActive(false);
            gobj_SystemMail.SetActive(true);
            gobj_PrivateMail.SetActive(false);
        }
        /// <summary>
        /// 開啟私人信件
        /// </summary>
        public void OnPrivateMailsBoxClick()
        {
            SlotSoundManager.bSndRef.PlaySoundEffect(ReferenceCenter.Ref.CommonMu.Container, SlotSoundManager.eAudioClip.Snd_ComClick1.ToString());
            transform.Find("Ani_Box/Gobj_BtnBox/UIBtn_System/Image").gameObject.SetActive(false);
            transform.Find("Ani_Box/Gobj_BtnBox/UIBtn_Private/Image").gameObject.SetActive(true);
            gobj_SystemMail.SetActive(false);
            gobj_PrivateMail.SetActive(true);
            
        }
    }

}
