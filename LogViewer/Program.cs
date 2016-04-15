using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LogViewer
{
    class Program
    {
        static void Main(string[] args)
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://*:11111");
            while (listener.IsListening)
            {
                //ожидаем входящие запросы
                HttpListenerContext context = listener.GetContext();

                //получаем входящий запрос
                HttpListenerRequest request = context.Request;

                var query = request.QueryString;
                //if (query)
                {

                }
                string responseString = @"<!DOCTYPE HTML>
                    <html><head></head><body>
                    <form method=""post"" action=""say"">
                    <p><b>Name: </b><br>
                    <input type=""text"" name=""myname"" size=""40""></p>
                    <p><input type=""submit"" value=""send""></p>
                    </form></body></html>";

                //отправка данных клиенту
                HttpListenerResponse response = context.Response;
                response.ContentType = "text/html; charset=UTF-8";
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                //response.ContentLength = buffer.Length;

                using (Stream output = response.OutputStream)
                {
                    output.Write(buffer, 0, buffer.Length);
                }
            }
        }
    }
}
