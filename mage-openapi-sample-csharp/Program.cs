using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;


namespace mage_openapi_sample_csharp
{
    class Program
    {
        private static readonly char[] constant =
        {
            '0','1','2','3','4','5','6','7','8','9', 'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z','A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z'
        };

        public static string GenerateRandom(int Length)
        {
            System.Text.StringBuilder newRandom = new System.Text.StringBuilder(62);
            Random rd = new Random();
            for (int i = 0; i < Length; i++)
            {
                newRandom.Append(constant[rd.Next(62)]);
            }
            return newRandom.ToString();
        }

        public static string EncryptToSHA1(string str)
        {
            SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider();
            byte[] str1 = Encoding.UTF8.GetBytes(str);
            byte[] str2 = sha1.ComputeHash(str1);
            sha1.Clear();
            (sha1 as IDisposable).Dispose();
            var sb = new StringBuilder(str2.Length * 2);

            foreach (byte b in str2)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }

        public static Dictionary<string, string> GenerateHeader(string apiAuthPubkey, string appAuthSecretkey)
        {
            string apiAuthTimestamp = ((DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000).ToString();
            string apiAuthNounce = GenerateRandom(10);
            string tokenKey = apiAuthNounce + apiAuthTimestamp + appAuthSecretkey;
            Console.WriteLine(apiAuthTimestamp);
            Console.WriteLine(apiAuthNounce);
            Console.WriteLine(tokenKey);
            Dictionary<string, string> headerDict = new Dictionary<string, string>()
            {
                { "Api-Auth-nonce", apiAuthNounce },
                { "Api-Auth-pubkey", apiAuthPubkey },
                { "Api-Auth-timestamp", apiAuthTimestamp },
                { "Api-Auth-sign", EncryptToSHA1(tokenKey) }
            };
            foreach (var i in headerDict)
            {
                Console.WriteLine(i.Key);
                Console.WriteLine(i.Value);
            }
            return headerDict;
        }

        public static string PostUrl(string routing, string postData, string apiAuthPubkey, string appAuthSecretkey, string endPoint)
        {
            string result = "";
            string url = endPoint + routing;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Timeout = 5 * 1000;
            foreach (var i in GenerateHeader(apiAuthPubkey, appAuthSecretkey))
            {
                request.Headers[i.Key] = i.Value;
            }
            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                streamWriter.Write(postData);
            }
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                string content = new StreamReader(response.GetResponseStream()).ReadToEnd();

                Console.WriteLine(content);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.ToString());
            }
            return result;
        }

        public static string FileToBase64(string filePath)
        {
            string result;
            FileStream filestream = new FileStream(filePath, FileMode.Open);
            byte[] bt = new byte[filestream.Length];

            filestream.Read(bt, 0, bt.Length);
            result = Convert.ToBase64String(bt);
            filestream.Close();
            return result;
        }

        public static string SingleImageWithoutParams(string apiAuthPubkey, string appAuthSecretkey, string imgPath, string type, string endPoint)
        {
            Dictionary<string, object> postData = new Dictionary<string, object>()
            {
                { "img_base64", FileToBase64(imgPath) }
            };

            string result = PostUrl("/v1/document/ocr/" + type, JsonSerializer.Serialize(postData), apiAuthPubkey, appAuthSecretkey, endPoint);
            return result;
        }

        /// <summary>
        /// https://mage.uibot.com.cn/docs/latest/docUnderstanding/backend/api.html#operation/OcrService_OcrVerification
        /// </summary>
        public static string OcrVerification(string apiAuthPubkey, string appAuthSecretkey, string imgPath, int verificationFormat = 0, string endPoint = "")
        {
            Dictionary<string, object> postData = new Dictionary<string, object>()
            {
                { "format", verificationFormat },
                { "img_base64", FileToBase64(imgPath) }
            };

            string result = PostUrl("/v1/document/ocr/verification", JsonSerializer.Serialize(postData), apiAuthPubkey, appAuthSecretkey, endPoint);
            Console.WriteLine(result);
            return result;
        }

        /// <summary>
        /// https://mage.uibot.com.cn/docs/latest/docUnderstanding/backend/api.html#operation/OcrService_OcrLicenseNew
        /// </summary>
        public static string OcrLicense(string apiAuthPubkey, string appAuthSecretkey, string imgPath, string endPoint)
        {
            string result = SingleImageWithoutParams(apiAuthPubkey, appAuthSecretkey, imgPath, "license", endPoint);
            return result;
        }
        /// <summary>
        /// https://mage.uibot.com.cn/docs/latest/docUnderstanding/backend/api.html#operation/OcrService_OcrStamp
        /// </summary>
        public static string OcrStamp(string apiAuthPubkey, string appAuthSecretkey, string imgPath, string endPoint)
        {
            string result = SingleImageWithoutParams(apiAuthPubkey, appAuthSecretkey, imgPath, "stamp", endPoint);
            return result;
        }

        /// <summary>
        /// https://mage.uibot.com.cn/docs/latest/docUnderstanding/backend/api.html#operation/OcrService_OcrBillsNew
        /// </summary>
        public static string OcrBills(string apiAuthPubkey, string appAuthSecretkey, string imgPath, string endPoint)
        {
            string result = SingleImageWithoutParams(apiAuthPubkey, appAuthSecretkey, imgPath, "bills", endPoint);
            return result;
        }

        /// <summary>
        /// https://mage.uibot.com.cn/docs/latest/docUnderstanding/backend/api.html#operation/OcrService_OcrTableNew
        /// </summary>
        public static string OcrTable(string apiAuthPubkey, string appAuthSecretkey, string imgPath, string endPoint = "")
        {
            List<string> list = new List<string>
            {
                FileToBase64(imgPath)
            };

            Dictionary<string, object> postData = new Dictionary<string, object>()
            {
                { "img_base64", list }
            };

            string result = PostUrl("/v1/mage/ocr/table", JsonSerializer.Serialize(postData), apiAuthPubkey, appAuthSecretkey, endPoint);
            Console.WriteLine(result);
            return result;
        }

        /// <summary>
        /// https://mage.uibot.com.cn/docs/latest/docUnderstanding/backend/api.html#operation/OcrService_OcrTemplateRecognize
        /// </summary>
        public static string OcrTemplate(string apiAuthPubkey, string appAuthSecretkey, string imgPath, bool withStructInfo = true, bool withRawInfo = true, string endPoint = "")
        {
            Dictionary<string, object> postData = new Dictionary<string, object>()
            {
                { "with_struct_info", withStructInfo },
                { "with_raw_info", withRawInfo },
                { "img_base64", FileToBase64(imgPath) }
            };

            string result = PostUrl("/v1/mage/ocr/template", JsonSerializer.Serialize(postData), apiAuthPubkey, appAuthSecretkey, endPoint);
            Console.WriteLine(result);
            return result;
        }

        /// <summary>
        /// https://mage.uibot.com.cn/docs/latest/docUnderstanding/backend/api.html#operation/OcrService_OcrGeneralNew
        /// </summary>
        public static string OcrGeneral(string apiAuthPubkey, string appAuthSecretkey, string imgPath, bool withStructInfo = true, bool withCharInfo = true, string endPoint = "")
        {
            List<string> imgList = new List<string>
            {
                FileToBase64(imgPath)
            };
            Dictionary<string, object> postData = new Dictionary<string, object>()
            {
                { "with_struct_info", withStructInfo },
                { "with_char_info", withCharInfo },
                { "img_base64", imgList}
            };

            string result = PostUrl("/v1/mage/ocr/general", JsonSerializer.Serialize(postData), apiAuthPubkey, appAuthSecretkey, endPoint);
            Console.WriteLine(result);
            return result;
        }

        /// <summary>
        /// https://mage.uibot.com.cn/docs/latest/docUnderstanding/backend/api.html#operation/AIService_DocExtract
        /// </summary>
        public static string NlpDocextractCreate(string apiAuthPubkey, string appAuthSecretkey, string filePath, string endPoint = "")
        {
            Dictionary<string, object> postData = new Dictionary<string, object>()
            {
                { "file_base64", FileToBase64(filePath) }
            };

            string result = PostUrl("/v1/mage/nlp/docextract/create", JsonSerializer.Serialize(postData), apiAuthPubkey, appAuthSecretkey, endPoint);
            Console.WriteLine(result);
            return result;
        }

        /// <summary>
        /// https://mage.uibot.com.cn/docs/latest/docUnderstanding/backend/api.html#operation/AIService_DocExtract
        /// </summary>
        public static string NlpDocextractQuery(string apiAuthPubkey, string appAuthSecretkey, string taskId, string endPoint = "")
        {
            Dictionary<string, object> postData = new Dictionary<string, object>()
            {
                { "task_id", taskId }
            };

            string result = PostUrl("/v1/mage/nlp/docextract/query", JsonSerializer.Serialize(postData), apiAuthPubkey, appAuthSecretkey, endPoint);
            Console.WriteLine(result);
            return result;
        }

        /// <summary>
        /// https://mage.uibot.com.cn/docs/latest/docUnderstanding/backend/api.html#operation/AIService_GEOExtract
        /// </summary>
        public static string NlpGeoextract(string apiAuthPubkey, string appAuthSecretkey, string text, string endPoint = "")
        {
            Dictionary<string, object> postData = new Dictionary<string, object>()
            {
                { "text", text }
            };

            string result = PostUrl("/v1/mage/nlp/geoextract", JsonSerializer.Serialize(postData), apiAuthPubkey, appAuthSecretkey, endPoint);
            Console.WriteLine(result);
            return result;
        }

        /// <summary>
        /// https://mage.uibot.com.cn/docs/latest/docUnderstanding/backend/api.html#operation/ClassifierService_Classify
        /// </summary>
        public static string NlpDocumentClassify(string apiAuthPubkey, string appAuthSecretkey, string text, string endPoint = "")
        {
            Dictionary<string, object> postData = new Dictionary<string, object>()
            {
                { "doc", text }
            };

            string result = PostUrl("/v1/document/classify", JsonSerializer.Serialize(postData), apiAuthPubkey, appAuthSecretkey, endPoint);
            Console.WriteLine(result);
            return result;
        }

        /// <summary>
        /// https://mage.uibot.com.cn/docs/latest/docUnderstanding/backend/api.html#operation/AIService_TextMatch
        /// </summary>
        public static string NlpTextMatch(string apiAuthPubkey, string appAuthSecretkey, string text, string endPoint = "")
        {
            Dictionary<string, object> postData = new Dictionary<string, object>()
            {
                { "doc", text }
            };

            string result = PostUrl("/v1/mage/nlp/textmatch", JsonSerializer.Serialize(postData), apiAuthPubkey, appAuthSecretkey, endPoint);
            Console.WriteLine(result);
            return result;
        }

        /// <summary>
        /// https://mage.uibot.com.cn/docs/latest/docUnderstanding/backend/api.html#operation/ExtractorService_Extract
        /// </summary>
        public static string NlpDocumentExtract(string apiAuthPubkey, string appAuthSecretkey, string text, string endPoint = "")
        {
            Dictionary<string, object> postData = new Dictionary<string, object>()
            {
                { "doc", text }
            };

            string result = PostUrl("/v1/document/extract", JsonSerializer.Serialize(postData), apiAuthPubkey, appAuthSecretkey, endPoint);
            Console.WriteLine(result);
            return result;
        }


        /// <summary>
        /// https://mage.uibot.com.cn/docs/latest/docUnderstanding/backend/api.html#operation/ContractCompareService_ContractCompare
        /// </summary>
        public static string SolutionContractCompare(string apiAuthPubkey, string appAuthSecretkey, string fileComparePath, string fileCompareName, string fileBasePath, string fileBaseName, string endPoint = "")
        {
            Dictionary<string, object> postData = new Dictionary<string, object>()
            {
                { "file_compare", FileToBase64(fileComparePath) },
                { "file_base", FileToBase64(fileComparePath) },
                { "file_compare_name", fileCompareName },
                { "file_base_name", fileBaseName,
            };

            string result = PostUrl("/v1/mage/solution/contract/compare", JsonSerializer.Serialize(postData), apiAuthPubkey, appAuthSecretkey, endPoint);
            Console.WriteLine(result);
            return result;
        }

        /// <summary>
        /// https://mage.uibot.com.cn/docs/latest/docUnderstanding/backend/api.html#operation/ContractCompareService_GetContractCompareResultDetail
        /// </summary>
        public static string SolutionContractDetail(string apiAuthPubkey, string appAuthSecretkey, string taskId, string endPoint = "")
        {
            Dictionary<string, object> postData = new Dictionary<string, object>()
            {
                { "task_id", taskId }
            };

            string result = PostUrl("/v1/mage/solution/contract/detail", JsonSerializer.Serialize(postData), apiAuthPubkey, appAuthSecretkey, endPoint);
            Console.WriteLine(result);
            return result;
        }

        /// <summary>
        /// https://mage.uibot.com.cn/docs/latest/docUnderstanding/backend/api.html#operation/ContractCompareService_GetCompareResultFiles
        /// </summary>
        public static string SolutionContractFiles(string apiAuthPubkey, string appAuthSecretkey, string taskId, string endPoint = "")
        {
            Dictionary<string, object> postData = new Dictionary<string, object>()
            {
                { "task_id", taskId }
            };

            string result = PostUrl("/v1/mage/solution/contract/files", JsonSerializer.Serialize(postData), apiAuthPubkey, appAuthSecretkey, endPoint);
            Console.WriteLine(result);
            return result;
        }

        /// <summary>
        /// https://mage.uibot.com.cn/docs/latest/docUnderstanding/backend/api.html#operation/IDPService_CreateIdpTask
        /// </summary>
        public static string IdpFlowTaskCreate(string apiAuthPubkey, string appAuthSecretkey, string filePath, string fileName, string endPoint = "")
        {
            Dictionary<string, object> postData = new Dictionary<string, object>()
            {
                { "base64", FileToBase64(filePath) },
                { "name", fileName
            };

            string result = PostUrl("/v1/mage/solution/contract/compare", JsonSerializer.Serialize(postData), apiAuthPubkey, appAuthSecretkey, endPoint);
            Console.WriteLine(result);
            return result;
        }

        /// <summary>
        /// https://mage.uibot.com.cn/docs/latest/docUnderstanding/backend/api.html#operation/IDPService_GetIDPTaskDetail
        /// </summary>
        public static string IdpFlowTaskQuery(string apiAuthPubkey, string appAuthSecretkey, string taskId, string endPoint = "")
        {
            Dictionary<string, object> postData = new Dictionary<string, object>()
            {
                { "task_id", taskId }
            };

            string result = PostUrl("/v1/mage/idp/flow/task/query", JsonSerializer.Serialize(postData), apiAuthPubkey, appAuthSecretkey, endPoint);
            Console.WriteLine(result);
            return result;
        }


        static void Main(string[] args)
        {
            Console.WriteLine("");
        }
    }
}
