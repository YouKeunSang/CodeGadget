/*
    Author  : You Keun Sang
    Date    : 2015.12.16
    Note    : you can't use rich text, cause it process 1 char at a time but rich text need whole meta tag or it just garbage char
              maybe it is possible support rich text with it!!! (in next version)
*/
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.EventSystems;
public struct DIALOG_EVENT
{
    public int pos;
    public string command;
    public string value;
}
[RequireComponent(typeof(Text))]
public class DialogWindowCore : MonoBehaviour {
    public float displaySpeed = 0.1f; //elapse time for printing one char
    public bool isLineFill = true; //false: if next word will extend width, next word will place at next line. true: break word to fill line width
    public bool isNonstop = false;
    public bool isScroll = false; // false: clear text box if it is full. true: erase first line and add new line
    public UnityEvent onTextFull;
    public UnityEvent onDialogFinished;

    Text _displayText;
    bool _isWork = false;
    RectTransform _rect;
    TextGenerationSettings _tgs;
    TextGenerator _tgen;
    LinkedList<DIALOG_EVENT> _events = new LinkedList<DIALOG_EVENT>();
    string _pureStr;
    int _maxLines;
    List<string> _lines;
    int _lineIdx;
    int _charPos;
    int _ignoreStop;
    int _consumedChar;

    bool _isDialogFinished = false;

    //debug variable
    string test = "abcde [speed:0.5]fghij klmno [speed:0.1]pqrst uvwxy zABCD EFGHI JKLMN OPQRS TUVWX YZ";
    void Start()
    {
        
        _rect = GetComponent<RectTransform>();
        _displayText = GetComponent<Text>();     
        _displayText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _displayText.verticalOverflow = VerticalWrapMode.Truncate;
        
        _tgs = _displayText.GetGenerationSettings(new Vector2(_rect.rect.width, _rect.rect.height));
        _tgen = new TextGenerator();
        _tgs.verticalOverflow = VerticalWrapMode.Overflow;
        if (isLineFill)
        {
            _tgs.horizontalOverflow = HorizontalWrapMode.Overflow;
        }
        else
        {
            _tgs.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        //Play();
    }
    public void Play(string dialog)
    {
        _displayText.text = "";
        _pureStr = ExtractEvent(dialog);
        _tgen.Populate(_pureStr, _tgs);
        _maxLines = (int)(_rect.rect.height / _displayText.preferredHeight);
        _lines = new List<string>();
        if (isLineFill)
        {
            int _startPos = 0;
            float _width = 0;
            for(int i=0;i<_tgen.characters.Count;i++)
            {
                _width += _tgen.characters[i].charWidth;
                if(_width > _rect.rect.width)
                {
                    string _line = _pureStr.Substring(_startPos, i - _startPos);
                    _lines.Add(_line);
                    _startPos = i;
                    _width = _tgen.characters[i].charWidth;
                }
            }
            //make leftovers to another line.
            if(_startPos < _tgen.characters.Count-1)
            {
                string _line = _pureStr.Substring(_startPos);
                _lines.Add(_line);
            }
        }
        else
        {
            for(int i=0 ; i < _tgen.lineCount ; i++)
            {
                if (i == _tgen.lineCount - 1)
                {
                    string _line = _pureStr.Substring(_tgen.lines[i].startCharIdx);
                    _lines.Add(_line);
                }
                else
                {
                    int _len = _tgen.lines[i+1].startCharIdx - _tgen.lines[i].startCharIdx;
                    string _line = _pureStr.Substring(_tgen.lines[i].startCharIdx, _len);
                    _lines.Add(_line);
                }
            }
        }
        _lineIdx = 0;
        _charPos = 0;
        _ignoreStop = 0;
        _consumedChar = 0;
        _isWork = true;
        _isDialogFinished = false;
        StartCoroutine(Display());
    }
    public void ContinueDialog()
    {
        if (!_isDialogFinished)
        {
            if (isScroll)
            {
                _displayText.text = _displayText.text.Remove(0, _displayText.text.IndexOf('\n') + 1);
            }
            else
            {
                //print("continu");
                _displayText.text = "";
            }
            _isWork = true;
            StartCoroutine(Display());
        }
    }

    IEnumerator Display()
    {
        while(_isWork)
        {
            //1.check event
            if( 0 <_events.Count && _events.First.Value.pos == _consumedChar)
            {
                yield return StartCoroutine(ProcessEvent(_events.First.Value));
                _events.RemoveFirst();
            }

            //2.show char one by one!
            _displayText.text += _lines[_lineIdx][_charPos++];
            _consumedChar++;

            //3.check end of one line
            if (_lines[_lineIdx].Length == _charPos)
            {
                _displayText.text += '\n';
                _lineIdx++;
                _charPos = 0;

                //4.check if it is end of dialog
                if(_lines.Count == _lineIdx)
                {
                    //print("dialog end");
                    _isDialogFinished = true;
                    if (null != onDialogFinished)
                    {
                        onDialogFinished.Invoke();
                    }
                    break;
                }
                if (isScroll)
                {
                    if(_lineIdx >= _maxLines)
                    {
                        if (isNonstop)
                        {
                            ContinueDialog();
                        }
                        if (null != onTextFull)
                        {
                            onTextFull.Invoke();
                        }
                        break;
                    }
                }
                else
                {
                    //5.check text box is full
                    if (0 == _lineIdx % _maxLines && _ignoreStop < _lineIdx)
                    {
                        if (isNonstop)
                        {
                            ContinueDialog();
                        }
                        //print("screen full");
                        _ignoreStop = _lineIdx;
                        if (null != onTextFull)
                        {
                            onTextFull.Invoke();
                        }
                        break;
                    }
                }
            }
           
            yield return new WaitForSeconds(displaySpeed);
        }
    }

    string ExtractEvent(string str)
    {
        int _cmdStartIdx;
        int _cmdEndIdx;

        do
        {
            _cmdStartIdx = str.IndexOf('[');
            _cmdEndIdx = str.IndexOf(']');
            if(-1 != _cmdStartIdx && -1 != _cmdEndIdx)
            {
                int _len = _cmdEndIdx - _cmdStartIdx;
                string _cmd = str.Substring(_cmdStartIdx+1, _len-1);    //avoid '[' & ']' separator
                string[] _queue = _cmd.Split(':');

                DIALOG_EVENT _event = new DIALOG_EVENT();
                _event.pos = _cmdStartIdx;
                _event.command = _queue[0];
                _event.value = _queue[1];
                _events.AddLast(_event);

                str = str.Remove(_cmdStartIdx, _len+1);
            }
        } while (-1 != _cmdStartIdx && -1 != _cmdEndIdx);

        //show rich text warning
        _cmdStartIdx = str.IndexOf('<');
        _cmdEndIdx = str.IndexOf('>');
        if (-1 != _cmdStartIdx && -1 != _cmdEndIdx)
        {
            Debug.LogWarning("This Dialog engine doesn't support RichText");
        }

        return str;
    }
    protected virtual IEnumerator ProcessEvent(DIALOG_EVENT e)
    {
        float _fVal;

        switch(e.command.Trim())
        {
            case "pause":
                if (!float.TryParse(e.value, out _fVal))
                {
                    Debug.LogError("event can't parse:" + e);
                }
                else
                {
                    yield return new WaitForSeconds(_fVal);
                }
                break;
            case "speed":
                if (!float.TryParse(e.value, out _fVal))
                {
                    Debug.LogError("event can't parse:" + e);
                }
                else
                {
                    displaySpeed = _fVal;
                }
                break;
            default:
                Debug.LogError("undefined dialog event:" + e.command);
                break;
        }
    }
}
