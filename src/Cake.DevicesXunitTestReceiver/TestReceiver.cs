using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Core.Annotations;
using Cake.Core.IO;

namespace Cake.DevicesXunitTestReceiver
{
    /// <summary>
    /// Contains methods pertaining to collecting results from devices.xunit test hosts running on devices.
    /// </summary>
    [CakeAliasCategory("DeviceXunitTestReceiver")]
    public static class TestReceiver
    {
        /// <summary>
        /// Listens for a connection from the device running the tests,
        /// it then processes the results and returns either true or false depending on if all tests have passed.
        /// </summary>
        /// <param name="context">Cake context.</param>
        /// <param name="port">Port to listen on.</param>
        /// <returns></returns>
        [CakeMethodAlias]
        public static Task<bool> LaunchEmbeddedTestsReceiver(ICakeContext context, int port)
        {
            var testsPassedCompletionSource = new TaskCompletionSource<bool>();
            
            AcceptNextConnectionThenExit(context, IPAddress.Loopback, port,
                async tcpClient =>
                {
                    var clientEndPoint = tcpClient.Client.RemoteEndPoint.ToString();
                    Console.WriteLine("Received connection request from "
                                      + clientEndPoint);
                    try 
                    {
                        var networkStream = tcpClient.GetStream();
                        var reader = new StreamReader(networkStream);
                        var lastDataReceived = "";
                        while (true)
                        {
                            var testData = await reader.ReadLineAsync();
                            if (testData != null)
                            {
                                if (testData.Contains("[FAIL]"))
                                {
                                    context.Warning(testData);
                                }
                                else
                                {
                                    context.Information(testData);
                                }
                                lastDataReceived = testData;
                            }
                            else // Client closed connection
                            {
                                if (tcpClient.Connected)
                                    tcpClient.Close();

                                var result = lastDataReceived.Contains("Failed: 0");
                                testsPassedCompletionSource.SetResult(result);
                            }
                        }
                    }
                    catch (Exception e) 
                    {
                        context.Error(e.Message);
                        if (tcpClient.Connected)
                            tcpClient.Close();

                        testsPassedCompletionSource.SetException(e);
                    }
                });
            return testsPassedCompletionSource.Task;
        }

        private static void AcceptNextConnectionThenExit(ICakeContext context, IPAddress ipAddress, int port, Func<TcpClient, Task> processTests)
        {
            var listener = new TcpListener(ipAddress, port);
            listener.Start();
            context.Information($"Test receiver is now running on {ipAddress}:{port}");
            Task.Run(
                async () =>
                {
                    var tcpClient = await listener.AcceptTcpClientAsync();
                    await processTests(tcpClient);
                    listener.Stop();
                });
        }
    }
}