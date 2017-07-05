using UnityEngine;
using UnityEngine.UI;
using System.Collections;
[RequireComponent(typeof(Text))]
public class MultiLangLabel : MonoBehaviour {
    Text _text;
    string _originText;
    DialogCtrl _dictionary;
	// Use this for initialization
	void Start () {
        _text = GetComponent<Text>();
        _dictionary = FindObjectOfType<DialogCtrl>();
        _originText = _text.text;
    }
	
	// Update is called once per frame
	void Update () {
	
	}
    public void RequestTranslate()
    {
        if(null  == _text)
        {
            _text = GetComponent<Text>();
        }
        if(null  == _originText)
        {
            _originText = _text.text;
        }
        if(null == _dictionary)
        {
            _dictionary = FindObjectOfType<DialogCtrl>();
        }

        string _tranLabel = _dictionary.LookupDic(_originText);
        if(!string.IsNullOrEmpty(_tranLabel))
        {
            _text.text = _tranLabel;
        }
        else
        {
            _text.text = _originText;
        }
    }
}
