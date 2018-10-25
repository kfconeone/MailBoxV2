using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using uTools;

public class BouncingText : MonoBehaviour {

    public RectTransform boundObject;
    uTweenPosition tweener;
    public float paddingFrom = 0;
    public float paddingEnd = 0;
    private void OnEnable()
    {
        if (tweener == null)
        {
            tweener = GetComponent<uTweenPosition>();

        }

        if (GetComponent<Text>().preferredWidth > boundObject.rect.width)
        {
            
            Bouncing();
        }
        
    }

    void Bouncing()
    {
        float bouncingPosX = boundObject.rect.width - GetComponent<Text>().preferredWidth;
        tweener.from = new Vector3(tweener.from.x + paddingFrom, tweener.from.y, 0f);
        tweener.to = new Vector3(bouncingPosX - paddingEnd, tweener.to.y, 0f);
        tweener.duration = (int)Mathf.Abs(bouncingPosX) / 30;
        tweener.enabled = true;

    }

    public void OnTweenFinished()
    {
        Invoke("Finishing", 1);
    }

    void Finishing()
    {
        tweener.enabled = false;
        tweener.ResetToBeginning();
        Invoke("Bouncing", 1);
    }
}
