using BestHTTP;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        //const string HOST = "http://entrance10.mobiusdice.com.tw/demoApi";
        const string HOST = "http://localhost:52673";
        string uri_GetMails = HOST + "/GetMailsV2";
        string uri_GetReward = HOST + "/GetRewardV2"; 
        string uri_SetMailLockOrNot = HOST + "/SetMailLockOrNotV2";

        //必要資訊
        string mAccount = "GM0000000006";
        string mGuid = "a06b9b4c-8ebf-4dc5-a694-4cec773c7236";

        //外部執行事件
        public Action<string> mMwssageBoxEvent;
        public Action<int, int, int,string> mGetRewardEvent;

        //目前點下去領取的信件
        GameObject currentMail;
        
        /// <summary>
        /// 開啟信箱
        /// </summary>
        public void OpenMailBox(string _account,string _guid,Action<string> _messageBoxEvent, Action<int, int, int, string> _getRewardEvent)
        {
            mAccount = _account;
            mGuid = _guid;
            mMwssageBoxEvent = _messageBoxEvent;
            mGetRewardEvent = _getRewardEvent;
            gameObject.SetActive(true);

        }

        /// <summary>
        /// 關閉信箱
        /// </summary>
        public void CloseMailBox()
        {
            mAccount = string.Empty;
            mGuid = string.Empty;
            mMwssageBoxEvent = null;

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
                if (mMwssageBoxEvent != null) mMwssageBoxEvent("與伺服器端連接失敗");
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
                if (mailDic["Sender"] == null || string.IsNullOrEmpty(mailDic["Sender"].ToString()))
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
                    mail.transform.Find("UIScroll_Name/Viewport/Content/UITxt_Name").GetComponent<Text>().text = bean.sender;
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
                    mail.transform.Find("UIScroll_Name/Viewport/Content/UITxt_Name").GetComponent<Text>().text = bean.sender;
                    mail.transform.Find("UITxt_Date").GetComponent<Text>().text = bean.sendingTime.ToString("yyyy.MM.dd");
                    mail.transform.Find("UITxt_Money").GetComponent<Text>().text = bean.reward;
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
            currentMail = _mail;

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
                if (mMwssageBoxEvent != null) mMwssageBoxEvent("與伺服器端連接失敗");
                return;
            }

            Debug.Log(response.DataAsText);
            JObject jsonResponse = JsonConvert.DeserializeObject<JObject>(response.DataAsText);
            if (!jsonResponse.GetValue("result").ToString().Contains("000"))
            {
                Debug.LogError("異常錯誤，請聯絡客服單位");
                if (mMwssageBoxEvent != null) mMwssageBoxEvent("異常錯誤，請聯絡客服單位");
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
                case 2:
                    bean.type = 0;
                    currentMail.transform.Find("UIBtn_Collect/Text").GetComponent<Text>().text = "刪除";
                    if(mGetRewardEvent != null) mGetRewardEvent(bean.type, (int)jsonResponse.GetValue("playerMoney"), (int)jsonResponse.GetValue("playerExp"), bean.reward);
                    break;

            }
            
        }

        /// <summary>
        /// 鎖定郵件 - 發送要求
        /// </summary>
        public void SetMailLockOrNot(GameObject _mail)
        {
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
                if (mMwssageBoxEvent != null) mMwssageBoxEvent("與伺服器端連接失敗");
                return;
            }

            Debug.Log(response.DataAsText);
            JObject jsonResponse = JsonConvert.DeserializeObject<JObject>(response.DataAsText);
            if (!jsonResponse.GetValue("result").ToString().Contains("000"))
            {
                Debug.LogError("異常錯誤，請聯絡客服單位");
                if (mMwssageBoxEvent != null) mMwssageBoxEvent("異常錯誤，請聯絡客服單位");
                return;
            }

            MailBean bean = currentMail.GetComponent<MailBean>();
            bean.isLock = (bool)jsonResponse.GetValue("isLock");
            
            currentMail.transform.Find("UIBtn_Unlock").gameObject.SetActive(bean.isLock);
            currentMail.transform.Find("UIBtn_Lock").gameObject.SetActive(!bean.isLock);
            
        }
        /// <summary>
        /// 開啟系統信件
        /// </summary>
        public void OnSystemMailsBoxClick()
        {        
            gobj_SystemMail.SetActive(true);
            gobj_PrivateMail.SetActive(false);
        }
        /// <summary>
        /// 開啟私人信件
        /// </summary>
        public void OnPrivateMailsBoxClick()
        {
            gobj_SystemMail.SetActive(false);
            gobj_PrivateMail.SetActive(true);
        }
    }

}
