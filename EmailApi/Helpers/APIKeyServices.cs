using Microsoft.AspNetCore.DataProtection;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using NotificationDomain;
using System.Text;

namespace EmailApi.Helpers
{
    public class APIKeyServices
    {
        
        
        public async Task<string> GeneratePublicKey(Secrets secreteProperties, SenderSettingsDTO senderSettings)
        {
            //string joiner = "#joi#ner#";
            string joiner = await CSharpScript.EvaluateAsync<string>(secreteProperties.ExecZero);
            StringBuilder sb = new StringBuilder();
            sb.Append(await EncryptData(senderSettings.Domain, secreteProperties));
            sb.Append(joiner);
            sb.Append(await EncryptData(senderSettings.Email, secreteProperties));
            sb.Append(joiner);
            sb.Append(await EncryptData(senderSettings.Port.ToString(), secreteProperties));
            sb.Append(joiner);
            sb.Append(await EncryptData(senderSettings.Password, secreteProperties));

            return sb.ToString();
        }


        public async Task<SenderSettingsDTO> RetrieveSenderSettings(Secrets secreteProperties, string publicKey)
        {
            if (string.IsNullOrWhiteSpace(publicKey))
            {
                return new SenderSettingsDTO();
            }
            var spliter = await CSharpScript.EvaluateAsync<string>(secreteProperties.ExecZero);
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

        public async Task<string> EncryptData(string password, Secrets secreteProp)
        {
            //====Encryption Algorithm========
            string randomstring = RandomStringGen(secreteProp.ExecOne, secreteProp.GetRandomGenSecrete);
            
            string passStr = "";
            for (int i = 0; i < password.Length; i++)
            {
                char c = password[i];
                int foundInd = Array.IndexOf((secreteProp.GetCharModel).ToCharArray(), c);
                var exGlobals = new ExecGlobals()
                {
                    X = passStr,
                    Y = secreteProp.GetReversedCharModel,
                    C = c
                };

                if (foundInd > -1)
                {
                    exGlobals.Z = secreteProp.GetReversedCharModel[foundInd].ToString();
                    passStr = await CSharpScript.EvaluateAsync<string>(secreteProp.ExecTwo, globals: exGlobals);
                }
                else
                {
                    
                    if (await CSharpScript.EvaluateAsync<bool>(secreteProp.ExecThree, globals:exGlobals))
                    {
                        passStr += secreteProp.ExecFour;
                    }
                    else
                    {
                        passStr = await CSharpScript.EvaluateAsync<string>(secreteProp.ExecFive, globals:exGlobals);
                    }

                }
            }

            var cLog = await CSharpScript.EvaluateAsync<int>(secreteProp.CutLogic);
            var exSix = new ExecGlobals(){ L = randomstring.Substring(0, cLog), M = passStr, R = randomstring.Substring(cLog), W = randomstring };
            string newString = await CSharpScript.EvaluateAsync<string>(secreteProp.ExecSix, globals:exSix);
            string passlen = password.Length > (cLog-secreteProp.TrimLogic) ? password.Length.ToString() : $"0{password.Length}";
            string trimmed = newString.Remove(newString.Length - 2, secreteProp.TrimLogic);
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
                string resEnPass = enPass.Replace("#PdR#", "@");
                int passLen = Convert.ToInt32(resEnPass.Substring(resEnPass.Length - 2, 2));
                string hashStr = resEnPass.Substring(11, passLen);
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
        public string ExecTwo { get; set; } = string.Empty;
        public string ExecThree { get; set; } = string.Empty;
        public string ExecFour { get; set; } = string.Empty;
        public string ExecFive { get; set; } = string.Empty;
        public string ExecSix { get; set; } = string.Empty;
        public int TrimLogic { get; set; }
        public string CutLogic { get; set; } = string.Empty;
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
