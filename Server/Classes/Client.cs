using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Classes
{
    public class Client
    {
        public string Token {  get; set; }
        public DateTime DateConnect { get; set; }
        public Client()
        {
            Random random = new Random();
            string chars = "QWERTYUIOPASDFGHJKLZXCVBNMqwertyuiopasdfghjklzxcvbnm0123456789";

            Token = new string(Enumerable.Repeat(chars, 15)
                                         .Select(x => x[random.Next(chars.Length)])
                                         .ToArray());

            DateConnect = DateTime.Now;
        }
    }
}
