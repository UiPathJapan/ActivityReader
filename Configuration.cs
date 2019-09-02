using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace UiPathTeam.ActivityReader
{
    public class Configuration
    {
        static private readonly string FOLDER1 = "UiPathTeam";
        static private readonly string FOLDER2 = "ActivityReader";
        static private readonly string FILENAME = "configuration.yml";

        static private Configuration _singleton;

        static Configuration()
        {
            _singleton = new Configuration();
        }

        static public Configuration GetInstance()
        {
            return _singleton;
        }

        private string _path;
        private Dictionary<string, Action> _loadActions;

        private Configuration()
        {
            var dirPath1 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), FOLDER1);
            var dirPath2 = Path.Combine(dirPath1, FOLDER2);
            _path = Path.Combine(dirPath2, FILENAME);
            _loadActions = new Dictionary<string, Action>();
            _loadActions.Add(TAG_CLASSRECORD, ReadClassRecord);
            _loadActions.Add(TAG_TYPESTRING, ReadTypeString);
            if (!File.Exists(_path))
            {
                if (!Directory.Exists(dirPath1))
                {
                    Directory.CreateDirectory(dirPath1);
                }
                if (!Directory.Exists(dirPath2))
                {
                    Directory.CreateDirectory(dirPath2);
                }
                CreateDefaults();
            }
        }

        public void CreateDefaults()
        {
            try
            {
                using (var sw = new StreamWriter(_path, false, Encoding.UTF8))
                {
                    WriteClassRecordDefaults(sw);
                    WriteTypeStringDefaults(sw);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("ERROR: Failed to save default configuration to file.");
                Console.Error.WriteLine("  Path: {0}", _path);
                Console.Error.WriteLine("  Reason: {0}", ex.Message);
            }
        }

        public void Load()
        {
            try
            {
                using (_sr = new StreamReader(_path, Encoding.UTF8))
                {
                    InitParse();
                    while (true)
                    {
                        SkipWS();
                        if (!ReadIdentifier(out string name))
                        {
                            break;
                        }
                        ReadColon();
                        if (_loadActions.TryGetValue(name, out Action loadValue))
                        {
                            loadValue();
                        }
                        else
                        {
                            throw new Exception("Unknown name: " + name);
                        }
                    }
                    if (!IsEOF)
                    {
                        throw new Exception("Extra contents.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("ERROR: Failed to load configuration from file.");
                Console.Error.WriteLine("  Path: {0}", _path);
                Console.Error.WriteLine("  Line: {0}", _line);
                Console.Error.WriteLine("  Reason: {0}", ex.Message);
            }
        }

        #region ClassRecord

        static private readonly string TAG_CLASSRECORD = "ClassRecord";
        static private readonly string TAG_ACTIVITYCLASSES = "ActivityClasses";

        private void WriteClassRecordDefaults(StreamWriter sw)
        {
            sw.WriteLine("{0}:", TAG_CLASSRECORD);
            sw.WriteLine("  {0}:", TAG_ACTIVITYCLASSES);
            foreach (var cn in ClassRecord.DEFAULT_ACTIVITY_CLASS_NAMES)
            {
                sw.WriteLine("    - {0}", cn);
            }
        }

        private void ReadClassRecord()
        {
            int indent1 = _indent;
            SkipToNextLine();
            SkipWS();
            int indent2 = _indent;
            if (indent1 < indent2)
            {
                do
                {
                    if (!ReadIdentifier(out string name))
                    {
                        throw new Exception("Syntax error.");
                    }
                    if (name == TAG_ACTIVITYCLASSES)
                    {
                        ReadColon();
                        SkipToNextLine();
                        SkipWS();
                        int indent3 = _indent;
                        if (indent2 < indent3)
                        {
                            var list = new List<string>();
                            do
                            {
                                ReadListItem(out string value);
                                list.Add(value.Trim());
                                SkipWS();
                            }
                            while (_indent == indent3);
                            ClassRecord.ActivityClassNames = list.ToArray();
                        }
                    }
                }
                while (_indent == indent2);
            }
            if (indent1 < _indent)
            {
                throw new Exception("Syntax error.");
            }
        }

        #endregion

        #region TypeString Configuration

        static private readonly string TAG_TYPESTRING = "TypeString";
        static private readonly string TAG_NAMESPACES = "Namespaces";

        private void WriteTypeStringDefaults(StreamWriter sw)
        {
            sw.WriteLine("{0}:", TAG_TYPESTRING);
            sw.WriteLine("  {0}:", TAG_NAMESPACES);
            foreach (var ns in TypeString.DEFAULT_NAMESPACES)
            {
                sw.WriteLine("    - {0}", ns);
            }
        }

        private void ReadTypeString()
        {
            int indent1 = _indent;
            SkipToNextLine();
            SkipWS();
            int indent2 = _indent;
            if (indent1 < indent2)
            {
                do
                {
                    if (!ReadIdentifier(out string name))
                    {
                        throw new Exception("Syntax error.");
                    }
                    if (name == TAG_NAMESPACES)
                    {
                        ReadColon();
                        SkipToNextLine();
                        SkipWS();
                        int indent3 = _indent;
                        if (indent2 < indent3)
                        {
                            var list = new List<string>();
                            do
                            {
                                ReadListItem(out string value);
                                list.Add(value.Trim());
                                SkipWS();
                            }
                            while (_indent == indent3);
                            TypeString.Namespaces = list.ToArray();
                        }
                    }
                }
                while (_indent == indent2);
            }
            if (indent1 < _indent)
            {
                throw new Exception("Syntax error.");
            }
        }

        #endregion

        #region Parser

        private StreamReader _sr;
        private char _c;
        private int _column;
        private int _line;
        private StringBuilder _buf = new StringBuilder();
        private int _indent;

        static private readonly char EOF = (char)0xffff;

        private void InitParse()
        {
            _c = (char)0;
            _column = 0;
            _line = 1;
            ReadChar();
        }

        private void ReadChar()
        {
            if (_c == EOF)
            {
                return;
            }
            else if (_c == '\n')
            {
                _line++;
                _column = 0;
                _indent = -1;
            }
            int c = _sr.Read();
            if (c == -1)
            {
                _c = EOF;
            }
            else if (c == '\r')
            {
                if (_sr.Peek() == '\n')
                {
                    _sr.Read();
                    _c = '\n';
                    _column++;
                }
                else
                {
                    _c = '\r';
                    _column++;
                }
            }
            else
            {
                if (c != ' ' && _indent == -1)
                {
                    _indent = _column;
                }
                _c = (char)c;
                _column++;
            }
        }

        private void SkipWS()
        {
            bool bAny = false;
            while (_c != EOF)
            {
                if (bAny)
                {
                    if (_c == '\n')
                    {
                        bAny = false;
                    }
                    ReadChar();
                }
                else if (_c == '#')
                {
                    ReadChar();
                    bAny = true;
                }
                else if (_c == ' ')
                {
                    ReadChar();
                }
                else
                {
                    break;
                }
            }
        }

        private bool ReadIdentifier(out string value)
        {

            if (Char.IsLetter(_c) || _c == '_')
            {
                _buf.Length = 0;
                _buf.Append(_c);
                ReadChar();
            }
            else
            {
                value = null;
                return false;
            }
            while (Char.IsLetterOrDigit(_c) || _c == '_')
            {
                _buf.Append(_c);
                ReadChar();
            }
            value = _buf.ToString();
            return true;
        }

        private void ReadColon()
        {
            while (_c == ' ')
            {
                ReadChar();
            }
            if (_c == ':')
            {
                ReadChar();
            }
            else
            {
                throw new Exception("Colon is missing.");
            }
            while (_c == ' ')
            {
                ReadChar();
            }
        }

        private void SkipToNextLine()
        {
            bool bAny = false;
            while (_c != '\n')
            {
                if (_c == EOF)
                {
                    throw new Exception("Unexpected end of file.");
                }
                else if (bAny)
                {
                    ReadChar();
                }
                else if (_c == '#')
                {
                    ReadChar();
                    bAny = true;
                }
                else if (_c == ' ')
                {
                    ReadChar();
                }
                else
                {
                    throw new Exception("Syntax error.");
                }
            }
            ReadChar();
        }

        private void ReadListItem(out string value)
        {
            if (_c == '-')
            {
                ReadChar();
            }
            else
            {
                throw new Exception("Syntax error.");
            }
            while (_c == ' ')
            {
                ReadChar();
            }
            _buf.Length = 0;
            while (_c != '\n')
            {
                if (_c == EOF)
                {
                    throw new Exception("Syntax error.");
                }
                _buf.Append(_c);
                ReadChar();
            }
            value = _buf.ToString();
            ReadChar();
        }

        private bool IsEOF
        {
            get
            {
                return _c == EOF;
            }
        }

#endregion
    }
}
