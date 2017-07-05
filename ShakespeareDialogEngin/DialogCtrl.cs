using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
public enum LANG
{
    _KR,
    _EN,
}
public struct DIALOG_MASTER_SCRIPT
{
    public string id;
    public List<string> dialogs;
}
public struct DIALOG_TEXT
{
    public string id;
    public string text;
}

public class DialogCtrl : MonoBehaviour {
    public Image portrait;
    LANG _curLang = LANG._KR;
    DialogWindowCore _dialogWnd;
    MultiLangLabel[] _labels; 
    Dictionary<string, string> _dialogTexts = new Dictionary<string, string>();
    Dictionary<string, string[]> _mstScripts = new Dictionary<string, string[]>();
    Dictionary<string, string> _transDic = new Dictionary<string, string>();

    const string _mstPostfix = "_master";
    const string _labelFile = "label";
    string[] _curMstScript;
    int _mstScrIdx = 0;
    bool _isWaitDialog = false;
    bool _isWork = false;

	// Use this for initialization
	void Start () {
        _dialogWnd = FindObjectOfType<DialogWindowCore>();
        _labels = FindObjectsOfType<MultiLangLabel>();
        ChangeLang(_curLang);
        Debug_setup();
    }
	
	// Update is called once per frame
	void Update () {

        while(_isWork && !_isWaitDialog)
        {
            _isWaitDialog = ProcessDialogCmd(_curMstScript[_mstScrIdx++]);
            if(_curMstScript.Length <= _mstScrIdx)
            {
                _isWork = false;
                break;
            }
        }
	}

    public void LoadChaptor(string chaptor)
    {
        _mstScripts.Clear();
        TextAsset _tmpTex = Resources.Load(chaptor + _mstPostfix) as TextAsset;
        string[] _lines = _tmpTex.text.Split('\n');
        foreach(string s in _lines)
        {
            string _temp = s.Trim();
            if (-1 != s.IndexOf('='))
            {
                string[] _scripts = _temp.Split('=');
                if (2 != _scripts.Length)
                {
                    Debug.LogWarning("abnormal master scrip");
                }
                else
                {
                    _mstScripts.Add(_scripts[0], _scripts[1].Split(','));
                }
            }
        }
        LoadDialog(chaptor,_curLang);
    }
    void LoadDialog(string chaptor,LANG lang)
    {
        TextAsset _tmpTex = Resources.Load(chaptor + lang.ToString().ToLower()) as TextAsset;
        string[] _lines = _tmpTex.text.Split('\n');
        _dialogTexts.Clear();

        foreach (string s in _lines)
        {
            string _temp = s.Trim();
            if (-1 != s.IndexOf(','))
            {
                string[] _scripts = _temp.Split(',');
                if (2 != _scripts.Length)
                {
                    Debug.LogWarning("abnormal text in language");
                }
                else
                {
                    _dialogTexts.Add(_scripts[0].Trim(), _scripts[1].Trim());
                }
            }
        }
    }
    public void PlayDialog(string id)
    {
        if(null != _dialogWnd && !_isWork)
        {
            if(!_mstScripts.TryGetValue(id,out _curMstScript))
            {
                Debug.LogError("invalid script id");
                return;
            }
            _mstScrIdx = 0;
            _isWaitDialog = false;
            _isWork = true;
        }
    }
    bool ProcessDialogCmd(string dialog)
    {
        bool _isWait = false;
        string[] _cmdNtext;
        string _text;
        if (-1 != dialog.IndexOf(':'))
        {
            _cmdNtext = dialog.Trim().Split(':');
            if (2 != _cmdNtext.Length)
            {
                Debug.LogWarning("abnormal command in master scrip");
                return false;
            }

            switch(_cmdNtext[0].Trim().ToLower())
            {
                case "dialog":
                    if (_dialogTexts.TryGetValue(_cmdNtext[1], out _text))
                    {
                        _dialogWnd.Play(_text);
                        _isWait = true;
                    }
                    else
                    {
                        Debug.LogWarning("text id can't find:" + _cmdNtext[1]);
                    }
                    break;
                case "cmd_setpic":
                    if (null != portrait)
                    {
                        portrait.overrideSprite = Resources.Load<Sprite>(_cmdNtext[1]);
                    }
                    break;
            }
        }
        return _isWait;
    }
    public string LookupDic(string origin)
    {
        //print("asking translate:" + origin);
        if(null != _transDic && 0 < _transDic.Count)
        {
            string _tranString;
            if(_transDic.TryGetValue(origin, out _tranString))
            {
                //print("find translate:" + _tranString);
                return _tranString;
            }
        }
        return null;
    }
    public void FinishOneSentence()
    {
        _isWaitDialog = false;
    }
    public void ChangeLang(LANG lang)
    {
        _transDic.Clear();
        TextAsset _tmpTex = Resources.Load(_labelFile + lang.ToString().ToLower()) as TextAsset;
        if (null != _tmpTex)
        {
            string[] _lines = _tmpTex.text.Split('\n');
            foreach (string s in _lines)
            {
                if (-1 != s.IndexOf('='))
                {
                    string[] _scripts = s.Split('=');
                    if (2 != _scripts.Length)
                    {
                        Debug.LogWarning("abnormal label translate format");
                    }
                    else
                    {
                        _transDic.Add(_scripts[0], _scripts[1]);
                    }
                }
            }
        }
        foreach (MultiLangLabel l in _labels)
        {
            l.RequestTranslate();
        }
    }
    
    //debug propose util func
    public void Debug_setup()
    {
        LoadChaptor("chaptor1");
    }
    public void Debug_toggleLang()
    {
        if (LANG._KR == _curLang)
        {
            _curLang = LANG._EN;
        }
        else
        {
            _curLang = LANG._KR;
        }
        ChangeLang(_curLang);
        LoadDialog("chaptor1", _curLang);
    }
}
