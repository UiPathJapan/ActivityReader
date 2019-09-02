using System;
using System.Linq;

namespace UiPathTeam.ActivityReader
{
    public class TypeString
    {
        static public readonly string[] DEFAULT_NAMESPACES =
        {
            "System",
            "System.Activities",
            "System.Collections.Generic",
            "System.Data",
        };

        static public string[] Namespaces
        {
            get;
            set;
        }

        static TypeString()
        {
            Namespaces = DEFAULT_NAMESPACES;
        }

        static public string Symplify(string t)
        {
            return new Parser(t).Run();
        }

        private class Parser
        {
            static private readonly string DIGITS = "0123456789";

            private string _t;
            private int _i;
            private char _c;

            public Parser(string t)
            {
                _t = t;
            }

            public string Run()
            {
                try
                {
                    Init();
                    var s = ReadType();
                    if (IsEnd)
                    {
                        return s;
                    }
                    else
                    {
                        Console.Error.WriteLine("ERROR: TypeString failed: Extra contents: {0}", _t.Substring(CurrentIndex));
                        return _t;
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("ERROR: TypeString failed: {0}", ex.Message);
                    return _t;
                }
            }

            private string ReadType()
            {
                var s1 = ReadName();
                var s2 = ReadTemplatePart();
                var s3 = ReadArrayPart();
                return string.Format("{0}{1}{2}", s1, s2, s3);
            }

            private string ReadName()
            {
                int pos1 = CurrentIndex;
                if (IsNameLeadChar)
                {
                    ReadChar();
                }
                else
                {
                    return "";
                }
                while (IsNameChar)
                {
                    ReadChar();
                }
                int pos2 = -1;
                while (_c == '.' || _c == '/') // It is observed that a slash is used as the most left hand separator of components 
                {
                    pos2 = CurrentIndex;
                    ReadChar();
                    if (IsNameLeadChar)
                    {
                        ReadChar();
                    }
                    else
                    {
                        throw new Exception("Name char is missing.");
                    }
                    while (IsNameChar)
                    {
                        ReadChar();
                    }
                }
                return pos2 > 0 && Namespaces.Contains(Substring(pos1, pos2)) ? Substring(pos2 + 1) : Substring(pos1);
            }

            private string ReadTemplatePart()
            {
                if (_c == '`')
                {
                    ReadChar();
                }
                else
                {
                    return "";
                }
                var n = DIGITS.IndexOf(_c);
                if (n > 0)
                {
                    ReadChar();
                }
                else
                {
                    throw new Exception("Non-zero Digit is missing.");
                }
                var d = DIGITS.IndexOf(_c);
                while (d > -1)
                {
                    n = n * 10 + d;
                    ReadChar();
                    d = DIGITS.IndexOf(_c);
                }
                if (_c == '<')
                {
                    ReadChar();
                }
                else
                {
                    throw new Exception("Less-than is missing.");
                }
                var s1 = ReadType();
                if (s1.Length == 0)
                {
                    throw new Exception("Template type is missing.");
                }
                for (int j = 1; j < n; j++)
                {
                    if (_c == ',')
                    {
                        ReadChar();
                    }
                    else
                    {
                        throw new Exception("Comma is missing.");
                    }
                    var s2 = ReadType();
                    if (s2.Length == 0)
                    {
                        throw new Exception("Template type is missing.");
                    }
                    s1 += "," + s2;
                }
                if (_c == '>')
                {
                    ReadChar();
                }
                else
                {
                    throw new Exception("Greater-than is missing.");
                }
                return string.Format("<{0}>", s1);
            }

            private string ReadArrayPart()
            {
                int pos = CurrentIndex;
                if (_c == '[')
                {
                    ReadChar();
                }
                else
                {
                    return "";
                }
                if (_c == ']')
                {
                    ReadChar();
                }
                else
                {
                    throw new Exception("Closing square bracket is missing.");
                }
                while (_c == '[')
                {
                    ReadChar();
                    if (_c == ']')
                    {
                        ReadChar();
                    }
                    else
                    {
                        throw new Exception("Closing square bracket is missing.");
                    }
                }
                return Substring(pos);
            }

            private void Init()
            {
                _i = 0;
                ReadChar();
            }

            private int CurrentIndex
            {
                get
                {
                    return _i - 1;
                }
            }

            private void ReadChar()
            {
                if (_i < _t.Length)
                {
                    _c = _t[_i++];
                }
                else
                {
                    _i = _t.Length + 1;
                    _c = (char)0;
                }
            }

            private bool IsEnd
            {
                get
                {
                    return _c == (char)0;
                }
            }

            private bool IsNameLeadChar
            {
                get
                {
                    return Char.IsLetter(_c) || _c == '_';
                }
            }

            private bool IsNameChar
            {
                get
                {
                    return Char.IsLetterOrDigit(_c) || _c == '_';
                }
            }

            private string Substring(int start)
            {
                return _t.Substring(start, CurrentIndex - start);
            }

            private string Substring(int start, int end)
            {
                return _t.Substring(start, end - start);
            }
        }
    }
}
