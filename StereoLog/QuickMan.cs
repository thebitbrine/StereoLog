﻿using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace TheBitBrine
{
    public class QuickMan
    {
        #region Server
        #region Server Init
        private Thread ServerThread;
        private HttpListener Listener;
        private IPAddress _Address;
        private int _Port = 1999;
        private Dictionary<string, Action<HttpListenerContext>> _Endpoints;

        public static int _MaxSimultaneousConnections = 20;
        private Semaphore sem = new Semaphore(_MaxSimultaneousConnections, _MaxSimultaneousConnections);

        /// <summary>
        /// Starts server then returns server's address.
        /// </summary>
        /// <param name="Address">IP Address</param>
        /// <param name="Port">Port</param>
        /// <param name="Endpoints">Endpoints dictionary</param>
        /// <returns></returns>
        public string Start(IPAddress Address, int Port, Dictionary<string, Action<HttpListenerContext>> Endpoints, int MaxSimultaneousConnections = 20)
        {
            _Port = Port;
            _Address = Address;
            _Endpoints = Endpoints;
            _MaxSimultaneousConnections = MaxSimultaneousConnections;
            ServerThread = new Thread(Listen) { IsBackground = false };
            ServerThread.Start();
            return $"http://{_Address}:{_Port}/";
        }

        /// <summary>
        /// Starts server on localhost then returns server's address.
        /// </summary>
        /// <param name="Port">Port</param>
        /// <param name="Endpoints">Endpoints dictionary</param>
        /// <returns></returns>
        public string Start(int Port, Dictionary<string, Action<HttpListenerContext>> Endpoints, int MaxSimultaneousConnections = 20)
        {
            _Port = Port;
            _Address = IPAddress.Parse(GetLocalIP());
            _Endpoints = Endpoints;
            _MaxSimultaneousConnections = MaxSimultaneousConnections;
            ServerThread = new Thread(Listen) { IsBackground = false };
            ServerThread.Start();
            return $"http://{_Address}:{_Port}/";
        }

        /// <summary>
        /// Starts server on localhost:1999 then returns server's address.
        /// </summary>
        /// <param name="Endpoints">Endpoints dictionary</param>
        /// <returns></returns>
        public string Start(Dictionary<string, Action<HttpListenerContext>> Endpoints, int MaxSimultaneousConnections = 20)
        {
            _Address = IPAddress.Parse(GetLocalIP());
            _Endpoints = Endpoints;
            _MaxSimultaneousConnections = MaxSimultaneousConnections;
            ServerThread = new Thread(Listen) { IsBackground = false };
            ServerThread.Start();
            return $"http://{_Address}:{_Port}/";
        }

        /// <summary>
        /// Stops server.
        /// </summary>
        public void Stop()
        {
            ServerThread.Abort();
            Listener.Stop();
        }

        private void Listen()
        {
            try
            {
                string Address = $"http://{_Address}:{_Port}/";
                AllowListener(Address);
                Listener = new HttpListener();
                Listener.Prefixes.Add(Address);
                Listener.Start();
                PrintLine($"INFO: Server running on {Address}");
            }
            catch (Exception ex)
            {
                PrintLine($"ERR: Server failed to start.\nCause: {ex.Message}");
            }

            while (Listener != null)
            {
                try
                {
                    sem.WaitOne();
                    HttpListenerContext context = Listener.GetContext();
                    new Thread(() => Process(context)) { IsBackground = true }.Start();
                }
                catch (Exception ex)
                {
                    PrintLine($"ERR: {ex.Message}");
                }
            }
        }
        #endregion
        #region Server Misc.

        public void PrintLine(string String)
        {
            Console.WriteLine(Tag(String));
        }

        public string Tag(string Text)
        {
            return "[" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + "] " + Text;
        }

        public string GetLocalIP()
        {
            using (System.Net.Sockets.Socket Socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, 0))
            {
                Socket.BeginConnect("8.8.8.8", 65530, null, null).AsyncWaitHandle.WaitOne(500, true);
                return (Socket.LocalEndPoint as System.Net.IPEndPoint)?.Address.ToString();
            }
        }

        //Check
        public void AllowListener(string URL)
        {
            try
            {
                var _URL = new Uri(URL);
                string command = $"http add urlacl url={_URL.Scheme}://{_URL.Host}:{_URL.Port}/ user=Everyone";
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("netsh", command) { WindowStyle = ProcessWindowStyle.Hidden, CreateNoWindow = true, Verb = "runas" });
            }
            catch { }
        }

        public void Respond(string Response, HttpListenerContext Context, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            try
            {
                Stream Input;
                Input = StringToStream(Response);
                Context.Response.ContentEncoding = Encoding.UTF8;
                Context.Response.ContentType =  "charset=utf-8";
                Context.Response.ContentLength64 = Input.Length;
                byte[] buffer = new byte[1024 * 16];
                int nbytes;
                while ((nbytes = Input.Read(buffer, 0, buffer.Length)) > 0)
                    Context.Response.OutputStream.Write(buffer, 0, nbytes);
                Input.Close();
                Context.Response.StatusCode = (int)statusCode;
            }
            catch
            {
                Context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
            Context.Response.OutputStream.Flush();
        }

        public void Respond(string Response, string ContentType, HttpListenerContext Context, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            try
            {
                Stream Input;
                //Encoding.

                Input = StringToStream(Response);

                Context.Response.ContentEncoding = Encoding.UTF8;
                Context.Response.ContentType = ((ContentType + ";").Replace(";;", ";")) + " charset=utf-8";

                Context.Response.ContentLength64 = Input.Length;
                byte[] buffer = new byte[1024 * 16];
                int nbytes;
                while ((nbytes = Input.Read(buffer, 0, buffer.Length)) > 0)
                    Context.Response.OutputStream.Write(buffer, 0, nbytes);
                Input.Close();
                Context.Response.StatusCode = (int)statusCode;
            }
            catch
            {
                Context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
            Context.Response.OutputStream.Flush();
        }

        public void Respond(Stream Response, string ContentType, HttpListenerContext Context, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            try
            {
                Context.Response.ContentEncoding = Encoding.UTF8;
                Context.Response.ContentType = ((ContentType + ";").Replace(";;", ";")) + " charset=utf-8";

                Context.Response.ContentLength64 = Response.Length;
                byte[] buffer = new byte[1024 * 16];
                int nbytes;
                while ((nbytes = Response.Read(buffer, 0, buffer.Length)) > 0)
                    Context.Response.OutputStream.Write(buffer, 0, nbytes);
                Response.Close();
                Context.Response.StatusCode = (int)statusCode;
            }
            catch
            {
                Context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
            Context.Response.OutputStream.Flush();
        }

        public void Respond(FileStream Response, string ContentType, HttpListenerContext Context, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            try
            {
                Context.Response.ContentEncoding = Encoding.UTF8;
                Context.Response.ContentType = ((ContentType + ";").Replace(";;", ";")) + " charset=utf-8";

                Context.Response.ContentLength64 = Response.Length;
                byte[] buffer = new byte[1024 * 16];
                int nbytes;
                while ((nbytes = Response.Read(buffer, 0, buffer.Length)) > 0)
                    Context.Response.OutputStream.Write(buffer, 0, nbytes);
                Response.Close();
                Context.Response.StatusCode = (int)statusCode;
            }
            catch
            {
                Context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
            Context.Response.OutputStream.Flush();
        }

        public Stream StringToStream(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public bool IsValidJson(string Response)
        {
            if (!string.IsNullOrWhiteSpace(Response))
            {
                if (Response.Contains(":"))
                {
                    if (Response.StartsWith("{") && Response.EndsWith("}"))
                        return true;
                    if (Response.StartsWith("[") && Response.EndsWith("]"))
                        return true;

                    return false;
                }
                else
                    return false;
            }
            else
                return false;
        }

        public class DefaultResponseFormat
        {
            public bool Ok;
            public string Status;
            public string HTTPStatusCode;
            public string Data;
        }

        #endregion
        #region Server Main
        private void Process(HttpListenerContext context)
        {
            try
            {
                try
                {
                    string RequestedEndpoint = context.Request.Url.AbsolutePath;
                    context.Response.Headers = new WebHeaderCollection();
                    context.Response.Headers.Add("Server", "QuickMan/1.0 on");

                    if (RequestedEndpoint != "/")
                    {
                        if (RequestedEndpoint.EndsWith("/"))
                            RequestedEndpoint = RequestedEndpoint.Remove(RequestedEndpoint.Length - 2, 1);

                        if (RequestedEndpoint.StartsWith("/"))
                            RequestedEndpoint = RequestedEndpoint.Remove(0, 1);

                        //if (RequestedEndpoint.Contains("/"))
                        //    RequestedEndpoint = RequestedEndpoint.Split('/').First();
                    }

                    if (RequestedEndpoint.Contains('?'))
                        RequestedEndpoint = RequestedEndpoint.Split('?')[0];

                    if (_Endpoints.Keys.Any(x => RequestedEndpoint.StartsWith(x))) {
                        var iEndpoint = _Endpoints.Keys.Where(x => RequestedEndpoint.StartsWith(x)).First();
                        _Endpoints[iEndpoint](context);
                    }
                    if (File.Exists($"HTML/{RequestedEndpoint}"))
                    {
                        Respond(File.ReadAllText($"HTML/{RequestedEndpoint}"), context, HttpStatusCode.OK);
                    }
                    else
                    {
                        context.Response.StatusDescription = "Endpoint not found";
                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    }
                }
                catch (Exception ex)
                {
                    context.Response.StatusDescription = ex.Message;
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                }

                context?.Response?.OutputStream.Close();
            }
            catch (Exception ex)
            {
                PrintLine($"ERR: {ex.Message}");
            }
            sem.Release();
        }

        #endregion
        #endregion

    }
}