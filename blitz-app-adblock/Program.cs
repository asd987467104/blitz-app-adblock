﻿using System;
using System.IO;
using System.Net;
using System.Text;
using asardotnet;
using blitz_app_adblock.Properties;

namespace blitz_app_adblock {
    class Program {
        private static string appPath =>
            $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\Programs\\Blitz\\resources";
        static void Main(string[] args) {
            bool noupdate = false;
            bool autoguest = false;

            foreach (string arg in args) {
                if(arg.ToLower() == "-noupdate") {
                    Console.WriteLine("Disabling Blitz auto update...");
                    noupdate = true;
                }
                if (arg.ToLower() == "-autoguest") {
                    Console.WriteLine("Enabling auto sign in as guest...");
                    autoguest = true;
                }
            }

            if (File.Exists($"{appPath}\\app.asar")) {
                Console.WriteLine("app.asar found!");

                try {
                    Console.WriteLine("Extracting...");
                    CopyFolder($"{appPath}\\app.asar.unpacked\\", $"{appPath}\\app\\");
                    var asar = new AsarArchive($"{appPath}\\app.asar");
                    var extractor = new AsarExtractor();
                    extractor.ExtractAll(asar, $"{appPath}\\app\\", true);
                } catch (IOException) {
                    Console.WriteLine("Error extracting files! Make sure Blitz app is closed before trying again.");
                    Console.ReadKey();
                    return;
                }
            
                Console.WriteLine("Downloading Easylists...");
                new WebClient().DownloadFile("https://easylist.to/easylist/easylist.txt", $"{appPath}\\app\\src\\easylist.txt");
                new WebClient().DownloadFile("https://easylist.to/easylist/easyprivacy.txt", $"{appPath}\\app\\src\\easyprivacy.txt");

                Console.WriteLine("Patching...");
                string fileToPatch = $"{appPath}\\app\\src\\createWindow.js";

                // copy adblocker lib to src
                File.WriteAllBytes($"{appPath}\\app\\src\\adblocker.umd.min.js", Encoding.UTF8.GetBytes(Resources.adblocker_umd_min));

                // start writing our payload to createWindow.js
                ModifyFileAtLine("session: true,", fileToPatch, 105);
                ModifyFileAtLine(

                "try {" +
                    "const fs = require('fs');" +
                    "const { FiltersEngine, Request} = require('./adblocker.umd.min.js');" +
                    "const filters = fs.readFileSync(require.resolve('./easylist.txt'), 'utf-8') + fs.readFileSync(require.resolve('./easyprivacy.txt'), 'utf-8');" +
                    "const engine = FiltersEngine.parse(filters);" +

                    "windowInstance.webContents.session.webRequest.onBeforeRequest({ urls:['*://*/*']}, (details, callback) => {" +
                        "const { match } = engine.match(Request.fromRawDetails({ url: details.url}));" +
                        "if (match == true) {" +
                            "log.info('BLOCKED:', details.url);" +
                            "callback({cancel: true});" +
                        "} else {" +
                            "callback({cancel: false});" +
                        "}" +
                    "});" +
                "} catch (error) {" +
                    "log.error(error);" +
                "}"

                , fileToPatch, 118);

                // optional features
                if (noupdate) ModifyFileAtLine("if (false) {", $"{appPath}\\app\\src\\index.js", 252);
                if (autoguest) { 
                    ModifyFileAtLine(

                        "autoGuest();" +
                        "function autoGuest() {" +
                            "var buttons = document.getElementsByTagName('button');" +
                            "for (var i = 0; i < buttons.length; i++) {" +
                                "if (buttons[i].getAttribute('label') == 'Login As Guest') {" +
                                    "buttons[i].click();" +
                                    "return;" +
                                "}" +
                            "}" +
                            "setTimeout(autoGuest, 1000);" +
                        "}"

                    , $"{appPath}\\app\\src\\preload.js", 18); 
                }

                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("Patching complete! GLHF :)");
            } else {
                Console.WriteLine("app.asar not found!");
            }
            Console.ReadKey();
        }

        static void ModifyFileAtLine(string newText, string fileName, int line_to_edit) {
            string[] arrLine = File.ReadAllLines(fileName);
            arrLine[line_to_edit - 1] = newText;
            File.WriteAllLines(fileName, arrLine);
            Console.WriteLine(fileName + ">>> Writing to line " + line_to_edit + ": " + newText);
        }

        static public void CopyFolder(string sourceFolder, string destFolder) {
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);
            string[] files = Directory.GetFiles(sourceFolder);
            foreach (string file in files) {
                string name = Path.GetFileName(file);
                string dest = Path.Combine(destFolder, name);
                File.Copy(file, dest, true);
            }
            string[] folders = Directory.GetDirectories(sourceFolder);
            foreach (string folder in folders) {
                string name = Path.GetFileName(folder);
                string dest = Path.Combine(destFolder, name);
                CopyFolder(folder, dest);
            }
        }
    }
}