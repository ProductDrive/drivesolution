using NotificationDomain;
using System.Text;

namespace WhatsAppApi.Helpers
{
    public class WhatsAppApiKeyServices
    {
        public string GeneratePublicKey(EmailApi.Helpers.Secrets secreteProperties, SenderSettingsDTO senderSettings)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(EncryptData(senderSettings.Email, secreteProperties));
            sb.Append(secreteProperties.ExecZero);
            sb.Append(EncryptData(senderSettings.Password, secreteProperties));

            return sb.ToString();
        }

        public SenderSettingsDTO? RetrieveSenderSettings(EmailApi.Helpers.Secrets secreteProperties, string publicKey)
        {
            if (string.IsNullOrWhiteSpace(publicKey))
            {
                return null;
            }
            var spliter = secreteProperties.ExecZero;
            var splitedKey = publicKey.Split(spliter).ToList();
            if (splitedKey.Count < 2)
            {
                return null;
            }

            return new SenderSettingsDTO
            {
                Email = DecryptData(splitedKey[0], secreteProperties),
                Password = DecryptData(splitedKey[1], secreteProperties),
                OnBehalf = true
            };
        }

        private string EncryptData(string data, EmailApi.Helpers.Secrets secreteProp)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return "";
            }

            string randomstring = RandomStringGen(secreteProp.ExecOne, secreteProp.GetRandomGenSecrete);
            string passStr = "";
            for (int i = 0; i < data.Length; i++)
            {
                char c = data[i];
                int foundInd = Array.IndexOf(secreteProp.GetCharModel.ToCharArray(), c);
                if (foundInd > -1)
                {
                    passStr += secreteProp.GetReversedCharModel[foundInd];
                }
                else
                {
                    if (c == secreteProp.ExecThree)
                    {
                        passStr += secreteProp.ExecFour;
                    }
                    else
                    {
                        passStr += c;
                    }
                }
            }
            string newString = randomstring.Insert(secreteProp.ExecTwo, passStr);
            string passlen = data.Length > (secreteProp.CutLogic - secreteProp.TrimLogic) ? data.Length.ToString() : $"0{data.Length}";
            string trimmed = newString.Remove(newString.Length - secreteProp.TrimLogic, 2);
            trimmed += passlen;
            return trimmed;
        }

        private string DecryptData(string enPass, EmailApi.Helpers.Secrets secreteOptions)
        {
            if (string.IsNullOrWhiteSpace(enPass))
            {
                return "";
            }
            try
            {
                var charModel = secreteOptions.GetCharModel;
                var reverseCharModel = secreteOptions.GetReversedCharModel;
                string resEnPass = enPass.Replace(secreteOptions.ExecFour, secreteOptions.ExecThree.ToString());
                int passLen = Convert.ToInt32(resEnPass.Substring(resEnPass.Length - secreteOptions.TrimLogic, 2));
                string hashStr = resEnPass.Substring(secreteOptions.CutLogic, passLen);
                string passStr = "";
                for (int i = 0; i < passLen; i++)
                {
                    char c = hashStr[i];
                    int foundInd = Array.IndexOf(reverseCharModel.ToCharArray(), c);
                    if (foundInd > -1)
                    {
                        passStr = passStr + charModel[foundInd];
                    }
                    else
                    {
                        passStr = passStr + c;
                    }
                }
                return passStr;
            }
            catch
            {
                return "";
            }
        }

        private string RandomStringGen(int num, string secretToUse)
        {
            string str = secretToUse;
            string randomstring = "";
            Random res = new Random();
            for (int i = 0; i < num; i++)
            {
                int x = res.Next(str.Length);
                randomstring = randomstring + str[x];
            }
            return randomstring;
        }
    }
}
