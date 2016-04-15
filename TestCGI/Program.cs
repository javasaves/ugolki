using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCGI
{
    class Program
    {
        static void Main(string[] args)
        {
            string response = @"HTTP/1.1 200 OK
Date: Wed, 11 Feb 2009 11:20:59 GMT
Server: Apache
X-Powered-By: PHP/5.2.4-2ubuntu5wm1
Last-Modified: Wed, 11 Feb 2009 11:20:59 GMT
Content-Language: ru
Content-Type: text/html; charset=utf-8
Content-Length: {0}
Connection: close

";
            response = "";
            string html = @"<!DOCTYPE html>
<html>
   <head>
      <title>2 + 2</title>
   </head>
   <body style=""background-color: #C5C5C5"">
      <p>
         <form action=""#"" method=""POST"">
<div style=""margin: 16px;"">
<span>A: </span><input style=""width: 152px;"" name=""a"" value=""2""/><br>
</div>
<div style=""margin: 16px;"">
<span>B: </span><input style=""width: 152px;"" name=""b"" value=""3""/>
</div>
<button style=""width: 192px; height: 32px; margin: 16px; background-color: #7F7;border-color:#4A4"" type=""submit"">Sum</button>
<br>{0}         
</form>
      </p>
   </body>
</html>";
            string resultHtml = @"<div style=""margin: 16px;"">
<span style=""width: 192px; height: 32px; font-size: 28; font-family: Arial;text-align:center; display:table-cell"">A + B = {0}</span>
</div>";
            Dictionary<string, string> arguments = new Dictionary<string, string>();
            try
            {

                if (args.Length > 0)
                {
                    arguments = args[0].Split('&').Select(x => x.Split('=')).ToDictionary(x => x[0].ToLower(), x => x[1]);
                }
                string a, b;
                if (arguments.TryGetValue("a", out a) && arguments.TryGetValue("b", out b))
                {
                    var data = string.Format(html, string.Format(resultHtml, double.Parse(a) + double.Parse(b)));
                    Console.Write(data);
                }
                else
                {
                    var data = string.Format(html, "");
                    Console.Write(data);
                }
            }
            catch (Exception ex)
            {
                Console.Write(@"HTTP/1.1 500 Internal Server Error
Date: Wed, 11 Feb 2009 11:20:59 GMT
Server: Apache
X-Powered-By: PHP/5.2.4-2ubuntu5wm1
Last-Modified: Wed, 11 Feb 2009 11:20:59 GMT
Content-Language: ru
Content-Type: text/html; charset=utf-8
Content-Length: 25
Connection: close

500 Internal Server Error");
            }
            
        }
    }
}
