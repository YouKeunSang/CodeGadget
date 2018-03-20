using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI에 대한 다국어 지원방법 구현
/// </summary>
[ExecuteInEditMode]
public class MultiLanguageLabel : Text {
    [HideInInspector]
    public LANGUAGE_TYPE langType;
    [HideInInspector]
    public UI_TEXT_TYPE uiType;
    [HideInInspector]
    public string uiID;
    [HideInInspector]
    public EssentialRectTransfom defaultTransform;

    public Dictionary<LANGUAGE_TYPE, EssentialRectTransfom> positionSnaps = new Dictionary<LANGUAGE_TYPE, EssentialRectTransfom>();

    protected override void OnEnable()
    {
        //실제 플레이 상황에서는 옵션값에서 읽어온 값으로 표시하고
        //에디터에서는 인스펙터에서 설정된 값으로 표시한다.
        if(Application.isPlaying)
        {
            langType = GameManager.instance.language;
        }
        OnChangeLanguage();
    }
    [ExecuteInEditMode]
    public void OnChangeLanguage(LANGUAGE_TYPE lang)
    {
        langType = lang;
        OnChangeLanguage();
    }
    [ExecuteInEditMode]
    public bool OnChangeLanguage()
    {
        if (!string.IsNullOrEmpty(uiID))
        {
            EssentialRectTransfom _trans;
            if(positionSnaps.TryGetValue(langType,out _trans))
            {
                _trans.RestoreRect(this);
            }
            else if(null != defaultTransform)
            {
                defaultTransform.RestoreRect(this);
            }

            switch (uiType)
            {
                case UI_TEXT_TYPE.OBJECT:
                    Language_objectInfo _objectInfo = DataManager.Instance.GetData<Language_objectInfo>(uiID);
                    if (null == _objectInfo)
                    {
                        text = uiID;
                        return false;
                    }
                    text = (string)_objectInfo.GetType().GetField(langType.ToString().ToLower()).GetValue(_objectInfo);
                    break;
                case UI_TEXT_TYPE.SYSTEM:
                    Language_systemInfo _systemInfo = DataManager.Instance.GetData<Language_systemInfo>(uiID);
                    if (null == _systemInfo)
                    {
                        text = uiID;
                        return false;
                    }
                    text = (string)_systemInfo.GetType().GetField(langType.ToString().ToLower()).GetValue(_systemInfo);
                    break;
                case UI_TEXT_TYPE.UI:
                    Language_uiInfo _info = DataManager.Instance.GetData<Language_uiInfo>(uiID);
                    if(null == _info)
                    {
                        text = uiID;
                        return false;
                    }
                    text = (string)_info.GetType().GetField(langType.ToString().ToLower()).GetValue(_info);
                    break;
                default:
                    text = uiID;
                    Debug.LogError("누군가 상의하지 않은 테이블을 만듬,잡아오셈");
                    break;
            }
        }
        return true;
    }
}

/// <summary>
/// rectTransform의 중요한 정보를 저장하고 언어가 바뀌었을때 원복해주는 클래스
/// </summary>
public class EssentialRectTransfom
{
    Vector2 _position;
    Vector2 _size;
    int _fontSize;
    public EssentialRectTransfom(MultiLanguageLabel label)
    {
        _position = label.rectTransform.anchoredPosition;
        _size = label.rectTransform.sizeDelta;
        _fontSize = label.fontSize;
    }
    public void RestoreRect(MultiLanguageLabel label)
    {
        label.rectTransform.anchoredPosition = _position;
        label.rectTransform.sizeDelta = _size;
        label.fontSize = _fontSize;
    }
}
