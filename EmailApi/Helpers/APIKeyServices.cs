using Microsoft.AspNetCore.DataProtection;
using NotificationDomain;
using System.Runtime.InteropServices;
using System.Text;

namespace EmailApi.Helpers
{
    public class APIKeyServices
    { 
        public string GeneratePublicKey(Secrets secreteProperties, SenderSettingsDTO senderSettings)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(EncryptData(senderSettings.Domain, secreteProperties));
            sb.Append(secreteProperties.ExecZero);
            sb.Append(EncryptData(senderSettings.Email, secreteProperties));
            sb.Append(secreteProperties.ExecZero);
            sb.Append(EncryptData(senderSettings.Port.ToString(), secreteProperties));
            sb.Append(secreteProperties.ExecZero);
            sb.Append(EncryptData(senderSettings.Password, secreteProperties));

            return sb.ToString();
        }


        public SenderSettingsDTO RetrieveSenderSettings(Secrets secreteProperties, string publicKey)
        {
            if (string.IsNullOrWhiteSpace(publicKey))
            {
                return new SenderSettingsDTO();
            }
            var spliter = secreteProperties.ExecZero;
            var splitedKey = publicKey.Split(spliter).ToList();
            if (splitedKey.Count < 4)
            {
                return new SenderSettingsDTO();
            }
            var senderSettings = new SenderSettingsDTO
            {
                Domain = DecryptData(splitedKey[0], secreteProperties),
                Email = DecryptData(splitedKey[1], secreteProperties),
                Password = DecryptData(splitedKey[3], secreteProperties)
            };
            var portToUse = 465;
            int.TryParse((DecryptData(splitedKey[2], secreteProperties)), out portToUse);
            senderSettings.Port = portToUse;
            return senderSettings;
        }

        private string EncryptData(string password, Secrets secreteProp)
        {
            //====Encryption Algorithm========
            string randomstring = RandomStringGen(secreteProp.ExecOne, secreteProp.GetRandomGenSecrete);
            string passStr = "";
            for (int i = 0; i < password.Length; i++)
            {
                char c = password[i];
                int foundInd = Array.IndexOf((secreteProp.GetCharModel).ToCharArray(), c);
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
            string passlen = password.Length > (secreteProp.CutLogic - secreteProp.TrimLogic) ? password.Length.ToString() : $"0{password.Length}";
            string trimmed = newString.Remove(newString.Length - secreteProp.TrimLogic, 2);
            trimmed += passlen;
            return trimmed;
        }


        private string DecryptData(string enPass, Secrets secreteOptions)
        {
            if (string.IsNullOrWhiteSpace(enPass))
            {
                return "";
            }
            try
            {
                var charModel = secreteOptions.GetCharModel;
                var reverseCharModel = secreteOptions.GetReversedCharModel;
                //=====Decryption Algorithm====
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
            catch (Exception ex)
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






















    public class Secrets
    {
        public string GetCharModel { get; set; } = string.Empty;
        public string GetReversedCharModel { get; set; } = string.Empty;
        public string GetRandomGenSecrete { get; set; } = string.Empty;
        public string ExecZero { get; set; } = string.Empty;
        public int ExecOne { get; set; }
        public int ExecTwo { get; set; }
        public char ExecThree { get; set; }
        public string ExecFour { get; set; } = string.Empty;
        public string ExecFive { get; set; } = string.Empty;
        public string ExecSix { get; set; } = string.Empty;
        public int TrimLogic { get; set; }
        public int CutLogic { get; set; }
    }

    public class ExecGlobals
    {
        public string X { get; set; } = string.Empty;
        public string Y { get; set; }= string.Empty;
        public string Z { get; set; } = string.Empty;
        public char C { get; set; }
        public string L { get; set; }= string.Empty;
        public string M { get; set; } = string.Empty;
        public string R { get; set; } = string.Empty;
        public string W { get; set; } = string.Empty;
    }
}
