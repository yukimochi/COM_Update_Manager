using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CM_UM_API
{
    using EntryMap = Dictionary<string, Dictionary<string, string>>;
    public class DescribeIni
    {
        private readonly EntryMap _map = new EntryMap();

        /// <summary>
        /// ファイル読み込み
        /// </summary>
        /// <param name="updateIni"></param>
        public DescribeIni(byte[] updateIni)
        {
            //Commentを除く
            var section = "";
            var rsection = new Regex("^[^;]*\\[(?<section>.*?)\\]");
            var comment = new Regex("^(?<ucmt>[^;]*?);(?<cmt>.*?)$");
            var keyvalue = new Regex("^(?<key>.*?)=(?<value>.*?)$");

            string line;
            var text = new StringReader(Encoding.Default.GetString(updateIni));
            while ((line = text.ReadLine()) != null)
            {
                //Section解析
                if (rsection.IsMatch(line))
                {
                    section = rsection.Match(line).Groups["section"].ToString();
                    _map[section] = new Dictionary<string, string>();
                }
                else if (string.IsNullOrEmpty(section))
                {
                }
                //Section登録されていればKey = Valueを検索する
                else if (comment.IsMatch(line))
                {
                    var uc = comment.Match(line).Groups["ucmt"].ToString();
                    if (keyvalue.IsMatch(uc))
                    {
                        AddParam(section,
                            keyvalue.Match(uc).Groups["key"].ToString(),
                            keyvalue.Match(uc).Groups["value"].ToString()
                        );
                    }
                }
                else if (keyvalue.IsMatch(line))
                {
                    AddParam(section,
                        keyvalue.Match(line).Groups["key"].ToString(),
                        keyvalue.Match(line).Groups["value"].ToString()
                    );

                }
            }
        }

        /// <summary>
        /// 登録されている値を取得する
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public string Get(string section, string key)
        {
            return HasKey(section, key) ? _map[section][key] : string.Empty;
        }

        private bool HasKey(string section, string key)
        {
            return _map.ContainsKey(section) && _map[section].ContainsKey(key);
        }

        private void AddParam(string section, string key, string value)
        {
            if (key.Trim().Length > 0)
            {
                _map[section].Add(key.Trim(), value.Trim());
            }
        }
    }
}
