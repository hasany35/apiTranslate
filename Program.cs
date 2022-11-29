using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Server;
using Microsoft.Data.SqlClient;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Nito.AsyncEx;

namespace tryToConnectSql
{
    class Program
    {
        private static string BaglantiAdresi = "connectionstring"; // connection string yazılır
        private static SqlConnection Baglanti = new SqlConnection(BaglantiAdresi);

        private static readonly string subscriptionKey = "...";         //*api key buraya yazılır"
        private static readonly string endpoint = "https://api.cognitive.microsofttranslator.com/";
        private static readonly string location = "northeurope";

        private static string myipMethod()
        {
            string bilgisayarAdi = Dns.GetHostName();
            string ipAdresi = Dns.GetHostByName(bilgisayarAdi).AddressList[0].ToString();
            return ipAdresi;
        }

        private static void translatesmth(string userid)
        {
            ////INSERT
            Console.WriteLine("çevirmek istediğiniz sözcügü giriniz");
            var wordTr = Console.ReadLine();

            var wordEng = AsyncContext.Run(() => MainAsync(wordTr));
            string theIp = myipMethod();
            SqlCommand insertCommand = new SqlCommand("INSERT INTO wordtable" +
                " (wordEnglish,wordTurkish,createDate,ip,useridtable)values" +
                " ('" + wordEng
                + "','" + wordTr + "', getdate() ," + "'" + theIp + "','" +userid+"')", Baglanti);
            using (SqlDataReader reader = insertCommand.ExecuteReader()) ;
            Console.WriteLine(wordEng.ToString());
            Baglanti.Close();
            Baglanti.Open();
            translatesmth(userid);

        }

        private static void joinmydb(string theid, string thepw)
        {

            //SqlCommand commandcount = new SqlCommand("select count(id) from myusers");
            //using (SqlDataReader reader = commandcount.ExecuteReader())
            //{
            //    while (reader.Read())
            //    {
            //        count=count+1;
            //    }
            //}
            //
            string userID = "0";
            string usertype = "guest";
            SqlCommand commandreadall = new SqlCommand("select * from myusers order by id", Baglanti);
            SqlDataReader reader = commandreadall.ExecuteReader();
            
                while (reader.Read())
                {
                    // write the data on to the screen    
                    string lineoftable=(String.Format("{0} \t | {1} \t | {2} \t | {3} \t | {4}",
                        reader[0], reader[1], reader[2], reader[3], reader[4]));

                ;
                        if (theid == reader[1].ToString() && thepw == reader[2].ToString())
                        {
                            Console.WriteLine("kullanıcı girişi başarılı");
                            userID=reader[0].ToString();
                            usertype = "user";
                            if (reader[4].ToString() == "y")
                            {
                                Console.WriteLine("admin olarak giriş yaptınız");
                                usertype = "admin";
                            }

                        }
                    
                }
            Baglanti.Close();
            Baglanti.Open();
            
            string userinip = myipMethod();
            string sucsessjo;
            if (usertype=="guest")
            {
                sucsessjo = "n";
            }
            else
            {
                sucsessjo = "y";
            }
            SqlCommand insertCommand2 = new SqlCommand("insert into denenenidpw(userip,tryToConnectTime,denenenKullaniciAdi,denenenSifre,sucsessJoin) " +
                                                            "values('" + userinip + "',getdate(),'" + theid + "','" + thepw + "','" + sucsessjo + "')", Baglanti);
            using (SqlDataReader reader2 = insertCommand2.ExecuteReader()) ;

            Console.WriteLine("girdiginiz kullanici bilgileriniz :"+theid+ " : " +thepw+ "   ip:"+ userinip+"     usertype:"+usertype+" .");
            Console.WriteLine(" devam etmek iicn tıkla");
            Console.ReadLine();

            if (usertype == "user")
            {
                translatesmth(userID);
            }
            else if (usertype == "admin")
            {
                translatesmth(userID);
            }
            else
            {
                Console.WriteLine("kullanıcı girişi başarısız tekrar deneyin");
                takeidpw();
            }
        }

        private static void takeidpw()
        {
            Console.WriteLine("kullanici adini giriniz ::: uyarı : kullanıcıadı toplam karakter 5 ile 50 arası: ve şifre 5 ile 10 karakter olmalı");
            var newfirstName = (Console.ReadLine());
            int numericValue;
            bool isNumber = int.TryParse(newfirstName, out numericValue);
            if (isNumber is true) { Console.WriteLine(" kullanıcı ismi hatalı : neden => kullanici adi özel karakter veya sayi icermez"); takeidpw(); }
            if (newfirstName.Length > 50 && newfirstName.Length <= 5) { Console.WriteLine(" ekleme başarısız : neden => en az 5 en fazla 50 karakter girebilirsiniz"); takeidpw(); }

            Console.WriteLine("şifrenizi giriniz");
            var newlastName = (Console.ReadLine());
            if (newlastName.Length > 50 && newlastName.Length <= 5) { Console.WriteLine(" ekleme başarısız : neden => en az 5 en fazla 50 karakter girebilirsiniz"); takeidpw(); }
            joinmydb(newfirstName, newlastName);
        }
        static void Main(string[] args)
        {
            try
            {

                Baglanti.ConnectionString = BaglantiAdresi;
                Baglanti.Open();

                takeidpw();



                Baglanti.Close();
                Console.WriteLine("Bağlantı Kapandı.");
                Console.ReadLine();
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }
            Console.ReadLine();
        }
        static async Task<string> MainAsync(string x)
        {
            string route = "/translate?api-version=3.0&from=en&to=tr"; 
            string textToTranslate = x;
            object[] body = new object[] { new { Text = textToTranslate } };
            var requestBody = JsonConvert.SerializeObject(body);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                // Build the request.
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(endpoint + route);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                request.Headers.Add("Ocp-Apim-Subscription-Region", location);

                // Send the request and get response.
                HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
                // Read response as a string.
                string result = await response.Content.ReadAsStringAsync();
                var root = JsonConvert.DeserializeObject<List<Root>>(result);

                return root[0].translations[0].text;
            }
        }

        public class Translation
        {
            public string text { get; set; }
            public string to { get; set; }
        }
        public class Root
        {
            public List<Translation> translations { get; set; }
        }

    }
}