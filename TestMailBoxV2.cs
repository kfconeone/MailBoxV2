using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMailBoxV2 : MonoBehaviour {

    public Kfc.MailBoxV2 mailBox;
	// Use this for initialization
	void Start () {
        mailBox.OpenMailBox("DV0000006583", "a48415e7-03c8-4f80-b87f-c6d92306c1de", null,null);

    }

}
