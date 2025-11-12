using PD.EmailSender.Helpers;
using PD.EmailSender.Helpers.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationWorker
{
    public static class Decoderr
    {
        public static string DecryptPassword(string enPass)
        {
            if (string.IsNullOrWhiteSpace(enPass))
            {
                return "";
            }

            try
            {
                char[] array = new char[78]
                {
                'A', 'a', 'B', 'b', 'C', 'c', 'D', 'd', 'E', 'e',
                'F', 'f', 'G', 'g', 'H', 'h', 'I', 'i', 'J', 'j',
                'K', 'k', 'L', 'l', 'M', 'm', 'N', 'n', 'O', 'o',
                'P', 'p', 'Q', 'q', 'R', 'r', 'S', 's', 'T', 't',
                'U', 'u', 'V', 'v', 'W', 'w', 'X', 'x', 'Y', 'y',
                'Z', 'z', '0', '1', '2', '3', '4', '5', '6', '7',
                '8', '9', '$', '#', '*', '&', '{', '}', '[', ']',
                '–', '=', '.', '(', ')', ';', '+', '/'
                };
                char[] array2 = new char[78]
                {
                '/', '+', ';', ')', '(', '.', '=', '-', ']', '[',
                '}', '{', '&', '*', '#', '$', '9', '8', '7', '6',
                '5', '4', '3', '2', '1', '0', 'z', 'Z', 'y', 'Y',
                'x', 'X', 'w', 'W', 'v', 'V', 'u', 'U', 't', 'T',
                's', 'S', 'r', 'R', 'q', 'Q', 'p', 'P', 'o', 'O',
                'n', 'N', 'm', 'M', 'l', 'L', 'k', 'K', 'j', 'J',
                'i', 'I', 'h', 'H', 'g', 'G', 'f', 'F', 'e', 'E',
                'd', 'D', 'c', 'C', 'b', 'B', 'a', 'A'
                };
                string text = enPass.Replace("#PdR#", "@");
                int num = Convert.ToInt32(text.Substring(text.Length - 2, 2));
                string text2 = text.Substring(11, num);
                string text3 = "";
                for (int i = 0; i < num; i++)
                {
                    char value = text2[i];
                    int num2 = Array.IndexOf(array2, value);
                    text3 = ((num2 <= -1) ? (text3 + value) : (text3 + array[num2]));
                }

                return text3;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "";
            }
        }

        public static List<MailAccount> GetMailAccounts()
        {
            var mailAccounts = new List<MailAccount>
            {
                new MailAccount { Domain = "schoolrex.ng", Port = 0, Email = "no-reply@schoolrex.ng", Password = "wywln6pkmn1U.$YY2V[P#PdR#jllk7qeyjaf14" },
                new MailAccount { Domain = "schoolrex.ng", Port = 465, Email = "no-reply@schoolrex.ng", Password = "xhkoqcoaqwxU.$YY2V[P#PdR#jllkvzqaepc14" },
                new MailAccount { Domain = "smtp.gmail.com", Port = 465, Email = "osewebo@gmail.com", Password = "almcfqmwwyzO$V4-OX8SN{[PZO*3ufo6nd16" },
                new MailAccount { Domain = "smtp.gmail.com", Port = 465, Email = "osewebo@gmail.com", Password = "5dt9sbsnommO$V4-OX8SN{[PZO*equr8az16" },
                new MailAccount { Domain = "smtp.gmail.com", Port = 465, Email = "eucoder960@gmail.com", Password = "hcubabrqtid)NOP8WV)0QTRO0VPs3au1oa16" },
                new MailAccount { Domain = "smtp.gmail.com", Port = 587, Email = "eucoder960@gmail.com", Password = "gxdt63a1sn1)NOP8WV)0QTRO0VPgoeo3xc16" },
                new MailAccount { Domain = "productdrive.com.ng", Port = 465, Email = "afee@productdrive.com.ng", Password = "k0nlcbyj2am/{[#PdR#HkmV[_ms56e8lz11" },
                new MailAccount { Domain = "smtp.gmail.com", Port = 465, Email = "info.admission@elizadeuniversity.edu.ng", Password = "gf5fmycy1bkP4W.$20O{$XX.$U{ppa242v16" },
                new MailAccount { Domain = "smtp.gmail.com", Port = 587, Email = "info.admission@elizadeuniversity.edu.ng", Password = "aeszm9zyovhP4W.$20O{$XX.$U{d4bh2qo16" },
                new MailAccount { Domain = "finnitech.com", Port = 465, Email = "tobby@finnitech.com", Password = "x434sambfvbp8({;yl&44upMKr78Ybvj6we618" },
                new MailAccount { Domain = "finnitech.com", Port = 0, Email = "tobby@finnitech.com", Password = "x434sambfvbp8({;yl&44upMKr78Ybvj6we618" },
                new MailAccount { Domain = "finnitech.com", Port = 587, Email = "tobby@finnitech.com", Password = "x434sambfvbp8({;yl&44upMKr78Ybvj6we618" },
                new MailAccount { Domain = "finnitech.com", Port = 2525, Email = "tobby@finnitech.com", Password = "x434sambfvbp8({;yl&44upMKr78Ybvj6we618" },
                new MailAccount { Domain = "finnitech.com", Port = 25, Email = "tobby@finnitech.com", Password = "x434sambfvbp8({;yl&44upMKr78Ybvj6we618" },
                new MailAccount { Domain = "smtp.gmail.com", Port = 587, Email = "tobbyumoh@gmail.com", Password = "h20apw8xy3eOQVU*ZU$24UZ68+Znjvra6x16" },
                new MailAccount { Domain = "smtp.gmail.com", Port = 587, Email = "umohjunior96@gmail.com", Password = "h17wmjg3fqgWTVT6WNU6S$S$+Y$lv10bww16" },
                new MailAccount { Domain = "projectdriveng.com.ng", Port = 0, Email = "admin@projectdriveng.com.ng", Password = "c6c0see01mlZ80-+IiJj#PdR#]2Yhtts44f13" },
                new MailAccount { Domain = "projectdriveng.com.ng", Port = 465, Email = "admin@projectdriveng.com.ng", Password = "ymsonw9s4feZ80-+IiJj#PdR#]2Y8h7ya5e13" },
                new MailAccount { Domain = "projectdriveng.com.ng", Port = 25, Email = "admin@projectdriveng.com.ng", Password = "vbs6vlazkegZ80-+IiJj#PdR#]2Ylh5hdjx13" },
                new MailAccount { Domain = "smtp.gmail.com", Port = 587, Email = "my@elizadeuniversity.edu.ng", Password = "7w7esgursd4[S#PdR#MlLkKtpes8jr08" },
                new MailAccount { Domain = "projectdriveng.com.ng", Port = 0, Email = "aafe@projectdriveng.com.ng", Password = "iozp5vj5n0ei3M4P{Gljvx5122609" },
                new MailAccount { Domain = "projectdriveng.com.ng", Port = 587, Email = "aafe@projectdriveng.com.ng", Password = "hfik69okab2i3M4P{Gljsqaodf909" },
                new MailAccount { Domain = "projectdriveng.com.ng", Port = 25, Email = "aafe@projectdriveng.com.ng", Password = "8c26i4njr7fi3M4P{Glj4269qmu09" }
            };
            return mailAccounts;
        }
    }

    public class MailAccount
    {
        public string Domain { get; set; }
        public int Port { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }


}
