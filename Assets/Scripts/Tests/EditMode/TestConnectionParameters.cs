using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Habitat.Tests.EditMode
{
    public class TestConnectionParameters
    {
        [Test]
        public void TestGetConnectionParameters()
        {
            string url;
            Dictionary<string, string> parameters;

            // Canonical case.
            url = "test?varA=true&varB=false";
            parameters = NetworkClient.GetConnectionParameters(url);
            Assert.AreEqual(parameters.Count, 2);
            Assert.IsTrue(parameters.ContainsKey("varA"));
            Assert.IsTrue(parameters.ContainsKey("varB"));
            Assert.AreEqual(parameters["varA"], "true");
            Assert.AreEqual(parameters["varB"], "false");

            // No connection parameter.
            url = "test";
            parameters = NetworkClient.GetConnectionParameters(url);
            Assert.AreEqual(parameters.Count, 0);

            // Nothing after "?".
            url = "test?";
            parameters = NetworkClient.GetConnectionParameters(url);
            Assert.AreEqual(parameters.Count, 0);

            // "?" only.
            url = "?";
            parameters = NetworkClient.GetConnectionParameters(url);
            Assert.AreEqual(parameters.Count, 0);

            // Multiple "?".
            url = "test?varA=true&varB=false?varC=true";
            parameters = NetworkClient.GetConnectionParameters(url);
            Assert.AreEqual(parameters.Count, 0);

            // Incomplete argument.
            url = "test?varA=true&varB";
            parameters = NetworkClient.GetConnectionParameters(url);
            Assert.AreEqual(parameters.Count, 1);
            Assert.AreEqual(parameters["varA"], "true");

            // Incomplete value.
            url = "test?varA=true&varB=";
            parameters = NetworkClient.GetConnectionParameters(url);
            Assert.AreEqual(parameters.Count, 2);
            Assert.AreEqual(parameters["varA"], "true");
            Assert.AreEqual(parameters["varB"], "");

            // Incomplete key.
            url = "test?varA=true&=false";
            parameters = NetworkClient.GetConnectionParameters(url);
            Assert.AreEqual(parameters.Count, 1);
            Assert.AreEqual(parameters["varA"], "true");

            // Two identical keys.
            url = "test?varA=true&varA=false";
            parameters = NetworkClient.GetConnectionParameters(url);
            Assert.AreEqual(parameters.Count, 1);
            Assert.AreEqual(parameters["varA"], "true,false");

            // Incomplete key and parameter.
            url = "test?varA=true&=";
            parameters = NetworkClient.GetConnectionParameters(url);
            Assert.AreEqual(parameters.Count, 1);
            Assert.AreEqual(parameters["varA"], "true");

            // Empty string.
            url = "";
            parameters = NetworkClient.GetConnectionParameters(url);
            Assert.AreEqual(parameters.Count, 0);

            // Null string.
            url = null;
            parameters = NetworkClient.GetConnectionParameters(url);
            Assert.AreEqual(parameters.Count, 0);
        }

        [Test]
        public void TestGetHostnameAndPort()
        {
            string url;
            Dictionary<string, string> parameters;

            // Valid cases.
            LogAssert.ignoreFailingMessages = false;

            // Canonical case.
            url = "test?server_hostname=HOST&server_port=1111&test=true";
            parameters = NetworkClient.GetConnectionParameters(url);
            {
                NetworkClient.GetServerHostnameAndPort(parameters, out var host, out var port);
                Assert.AreEqual(host, "HOST");
                Assert.AreEqual(port, 1111);
            }

            // Only hostname.
            url = "test?server_hostname=HOST&test=1111";
            parameters = NetworkClient.GetConnectionParameters(url);
            {
                NetworkClient.GetServerHostnameAndPort(parameters, out var host, out var port);
                Assert.AreEqual(host, "HOST");
                Assert.AreEqual(port, null);
            }

            // Only port.
            url = "test?test=HOST&server_port=1111";
            parameters = NetworkClient.GetConnectionParameters(url);
            {
                NetworkClient.GetServerHostnameAndPort(parameters, out var host, out var port);
                Assert.AreEqual(host, null);
                Assert.AreEqual(port, 1111);
            }

            // No parameter.
            url = "test";
            parameters = NetworkClient.GetConnectionParameters(url);
            {
                NetworkClient.GetServerHostnameAndPort(parameters, out var host, out var port);
                Assert.AreEqual(host, null);
                Assert.AreEqual(port, null);
            }

            // IP hostname.
            url = "test?server_hostname=127.0.0.1";
            parameters = NetworkClient.GetConnectionParameters(url);
            {
                NetworkClient.GetServerHostnameAndPort(parameters, out var host, out var port);
                Assert.AreEqual(host, "127.0.0.1");
                Assert.AreEqual(port, null);
            }

            // Null input
            {
                NetworkClient.GetServerHostnameAndPort(null, out var host, out var port);
                Assert.AreEqual(host, null);
                Assert.AreEqual(port, null);
            }

            // Invalid cases.
            LogAssert.ignoreFailingMessages = true;

            // Hostname with port. This is considered invalid.
            url = "test?server_hostname=127.0.0.1:1111";
            parameters = NetworkClient.GetConnectionParameters(url);
            {
                NetworkClient.GetServerHostnameAndPort(parameters, out var host, out var port);
                Assert.AreEqual(host, null);
                Assert.AreEqual(port, null);
            }

            // Invalid hostname.
            url = "test?server_hostname=test<>";
            parameters = NetworkClient.GetConnectionParameters(url);
            {
                NetworkClient.GetServerHostnameAndPort(parameters, out var host, out var port);
                Assert.AreEqual(host, null);
                Assert.AreEqual(port, null);
            }

            // Invalid port.
            url = "test?server_port=test";
            parameters = NetworkClient.GetConnectionParameters(url);
            {
                NetworkClient.GetServerHostnameAndPort(parameters, out var host, out var port);
                Assert.AreEqual(host, null);
                Assert.AreEqual(port, null);
            }

            // Negative port.
            url = "test?server_port=-100";
            parameters = NetworkClient.GetConnectionParameters(url);
            {
                NetworkClient.GetServerHostnameAndPort(parameters, out var host, out var port);
                Assert.AreEqual(host, null);
                Assert.AreEqual(port, null);
            }

            // Port > 65535.
            url = "test?server_port=1000000";
            parameters = NetworkClient.GetConnectionParameters(url);
            {
                NetworkClient.GetServerHostnameAndPort(parameters, out var host, out var port);
                Assert.AreEqual(host, null);
                Assert.AreEqual(port, null);
            }

            LogAssert.ignoreFailingMessages = false;
        }
    }
}
