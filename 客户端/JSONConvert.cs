using System.Collections.Generic;
using System.Text;


public static class JSONConvert
{
    #region Global Variables
    private static char[] _charary;
    private static int _aryend;

    #endregion

    #region JSON Deserialization
    
    private static JSONObject DeserializeSingletonObject(ref int left)
    {
        JSONObject localjson = new JSONObject();
        while (left <= _aryend)
        {
            char c = _charary[left];
            if (c == ' ' || c == '\r' || c == '\n' || c == '\t') 
            {
                left++;
                continue;
            }
            if (c == ',')
            {
                left++;
                continue;
            }
            char r = '\0';
            if (c == '\"' || c == '\'')
            {
                left++;
                r = c;
            }
            else if (c == '}')
            {
                left++;
                break;
            }
            int column = left;
            if (r == '\0')
            {
                while (_charary[column] != ':') column++;
            }
            else
            {
                while (!(_charary[column] == r && _charary[column - 1] != '\\' && _charary[column + 1] == ':')) column++;
            }
            string key = new string(_charary, left, column - left);         
            if (r == '\0')
                left = column + 1;
            else
                left = column + 2;
            c = _charary[left];
            while (c == ' ' || c == '\r' || c == '\n' || c == '\t')  
            {
                left++;
                c = _charary[left];
            }
            if (c == '\"' || c == '\'')    
            {
                left++;
                int strend = left;
                while (_charary[strend] != c || _charary[strend - 1] == '\\') strend++;
                localjson[key] = new string(_charary, left, strend - left);
                left = strend + 1;
            }
            else if (c == '{')
            {
                left++;
                localjson[key] = DeserializeSingletonObject(ref left);
            }
            else if (c == '[')   
            {
                left++;
                localjson[key] = DeserializeSingletonArray(ref left);
            }
            else
            {
                int comma = left;
                char co = _charary[comma];
                while (co != ',' && co != '}')
                {
                    comma++;
                    co = _charary[comma];
                }
                int em = comma - 1;
                co = _charary[em];
                while (co == ' ' || co == '\r' || co == '\n' || co == '\t')
                {
                    em--;
                    co = _charary[em];
                }
                localjson[key] = new string(_charary, left, em - left + 1);
                left = comma;
            }
        }
        return localjson;
    }
    
    private static JSONArray DeserializeSingletonArray(ref int left)
    {
        JSONArray jsary = new JSONArray();
        while (left <= _aryend)
        {
            char c = _charary[left];
            if (c == ' ' || c == '\r' || c == '\n' || c == '\t')  
            {
                left++;
                continue;
            }
            if (c == ',')
            {
                left++;
                continue;
            }
            if (c == ']')
            {
                left++;
                break;
            }
            if (c == '{') 
            {
                left++;
                jsary.Add(DeserializeSingletonObject(ref left));
            }
            else if (c == '[')     
            {
                left++;
                jsary.Add(DeserializeSingletonArray(ref left));
            }
            else if (c == '\"' || c == '\'')            
            {
                left++;
                int strend = left;
                while (_charary[strend] != c || _charary[strend - 1] == '\\') strend++;
                jsary.Add(new string(_charary, left, strend - left));
                left = strend + 1;
            }
            else
            {
                int comma = left;
                char co = _charary[comma];
                while (co != ',' && co != ']')
                {
                    comma++;
                    co = _charary[comma];
                }
                int em = comma - 1;
                co = _charary[em];
                while (co == ' ' || co == '\r' || co == '\n' || co == '\t')
                {
                    em--;
                    co = _charary[em];
                }
                jsary.Add(new string(_charary, left, em - left + 1));
                left = comma;
            }
        }
        return jsary;
    }

    #endregion

    #region Public Interface
    
    public static JSONObject DeserializeCharToObject(char[] input)
    {
        _charary = input;
        _aryend = _charary.Length - 1;
        while (_aryend > 0)
            if (_charary[_aryend] != '}')
                _aryend--;
            else
                break;
        int start = 0;
        while (start < _aryend)
            if (_charary[start] != '{')
                start++;
            else
                break;
        start++;
        if (_aryend < start + 1)
            return null;
        return DeserializeSingletonObject(ref start);
    }
    
    public static JSONObject DeserializeObject(string input)
    {
        return DeserializeCharToObject(input.ToCharArray());     //The first char must be '{'
    }
    
    public static JSONArray DeserializeCharsToArray(char[] input)
    {
        _charary = input;
        _aryend = _charary.Length - 1;
        while (_aryend > 0)
            if (_charary[_aryend] != ']')
                _aryend--;
            else
                break;
        int start = 0;
        while (start < _aryend)
            if (_charary[start] != '[')
                start++;
            else
                break;
        start++;
        if (_aryend < start + 1)
            return null;
        return DeserializeSingletonArray(ref start);
    }
    
    public static JSONArray DeserializeArray(string input)
    {
        return DeserializeCharsToArray(input.ToCharArray());
    }
    public static string SerializeObject(JSONObject jsonObject)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("{");
        foreach (KeyValuePair<string, object> kvp in jsonObject)
        {
            if (kvp.Value is JSONObject)
            {
                sb.Append(string.Format("\"{0}\":{1},", kvp.Key, SerializeObject((JSONObject)kvp.Value)));
            }
            else if (kvp.Value is JSONArray)
            {
                sb.Append(string.Format("\"{0}\":{1},", kvp.Key, SerializeArray((JSONArray)kvp.Value)));
            }
            else if (kvp.Value is string)
            {
                sb.Append(string.Format("\"{0}\":\"{1}\",", kvp.Key, kvp.Value));
            }
            else if (kvp.Value is int || kvp.Value is long)
            {
                sb.Append(string.Format("\"{0}\":{1},", kvp.Key, kvp.Value));
            }
            else
            {
                sb.Append(string.Format("\"{0}\":\"{1}\",", kvp.Key, ""));
            }
        }
        if (sb.Length > 1)
            sb.Remove(sb.Length - 1, 1);
        sb.Append("}");
        return sb.ToString();
    }
    
    public static string SerializeArray(JSONArray jsonArray)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("[");
        for (int i = 0; i < jsonArray.Count; i++)
        {
            if (jsonArray[i] is JSONObject)
            {
                sb.Append(string.Format("{0},", SerializeObject((JSONObject)jsonArray[i])));
            }
            else if (jsonArray[i] is JSONArray)
            {
                sb.Append(string.Format("{0},", SerializeArray((JSONArray)jsonArray[i])));
            }
            else if (jsonArray[i] is string)
            {
                sb.Append(string.Format("\"{0}\",", jsonArray[i]));
            }
            else
            {
                sb.Append(string.Format("\"{0}\",", ""));
            }

        }
        if (sb.Length > 1)
            sb.Remove(sb.Length - 1, 1);
        sb.Append("]");
        return sb.ToString();
    }

    #endregion
}


public class JSONObject : Dictionary<string, object>
{
    public void put(string key, string value)
    {
        this[key] = value;
    }

    public void put(string key, int value)
    {
        this[key] = value.ToString();

    }
}


public class JSONArray : List<object>
{ }
