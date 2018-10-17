﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClassLibraryCommon;
using CommonClassLibraryJD;
using Newtonsoft.Json;

namespace DCS_BIOS
{

    public static class DCSBIOSControlLocator
    {
        private static readonly object _lockObject = new object();
        private static DCSAirframe _airframe;
        private static string _jsonDirectory;
        private static bool _useNS430 = false;
        private static bool _NS430Loaded = false;
        private static bool _airFrameChanged;
        private static List<DCSBIOSControl> _dcsbiosControls = new List<DCSBIOSControl>();

        public static DCSBIOSControl GetControl(string controlId)
        {
            lock (_lockObject)
            {
                if (_airframe == DCSAirframe.KEYEMULATOR || _airframe == DCSAirframe.KEYEMULATOR_SRS)
                {
                    return null;
                }
                try
                {
                    //PrintDuplicateControlIdentifiers(_dcsbiosControls, true);
                    LoadControls();
                    return _dcsbiosControls.Single(controlObject => controlObject.identifier.Equals(controlId));
                }
                catch (InvalidOperationException ioe)
                {
                    throw new Exception("Check DCS-BIOS version. Failed to find control " + controlId + " for airframe " + Airframe.GetDescription() + " (" + Airframe.GetDescription() + ".json). Did you switch airframe type for the profile and have existing control(s) for the previous type saved?" + Environment.NewLine + ioe.Message);
                }
            }
        }

        static void PrintDuplicateControlIdentifiers(List<DCSBIOSControl> dcsbiosControls, bool printAll = false)
        {
            var result = new List<string>();
            var dupes = new List<string>();
            foreach (var dcsbiosControl in dcsbiosControls)
            {
                if (printAll)
                {
                    result.Add(dcsbiosControl.identifier);
                }

                //Debug.Print(dcsbiosControl.identifier);
                var found = false;
                foreach (var str in result)
                {
                    if (str.Trim() == dcsbiosControl.identifier.Trim())
                    {
                        found = true;
                    }
                }
                if (!found)
                {
                    result.Add(dcsbiosControl.identifier);
                }
                if (found)
                {
                    dupes.Add(dcsbiosControl.identifier);
                }

            }
            if (dupes.Count > 0)
            {
                var message = "Below is a list of duplicate identifiers found in the " + Airframe.GetDescription() + ".json profile (DCS-BIOS)\n";
                message = message + "The identifier must be unique, please correct the profile " + Airframe.GetDescription() + ".lua in the DCS-BIOS lib folder\n";
                message = message + "---------------------------------------------\n";
                foreach (var dupe in dupes)
                {
                    message = message + dupe + "\n";
                }
                message = message + "---------------------------------------------\n";
                Common.LogError(2000, message);
            }
        }
        
        public static DCSBIOSOutput GetDCSBIOSOutput(string controlId)
        {
            lock (_lockObject)
            {
                if (_airframe == DCSAirframe.KEYEMULATOR || _airframe == DCSAirframe.KEYEMULATOR_SRS)
                {
                    throw new Exception("DCSBIOSControlLocator.GetDCSBIOSOutput() Should not be called when only key emulator is active");
                }
                try
                {
                    var control = GetControl(controlId);
                    var dcsBIOSOutput = new DCSBIOSOutput();
                    dcsBIOSOutput.Consume(control);
                    return dcsBIOSOutput;
                }
                catch (InvalidOperationException ioe)
                {
                    throw new Exception("Check DCS-BIOS version. Failed to create DCSBIOSOutput based on control " + controlId + " for airframe " + Airframe.GetDescription() + " ( " + Airframe.GetDescription() + ".json)." + Environment.NewLine + ioe.Message);
                }
            }
        }

        public static void LoadControls(string jsonDirectory, DCSAirframe airframe, bool clearList = true)
        {
            LoadControls(jsonDirectory, airframe.GetDescription(), clearList);
        }

        public static void LoadControls(string jsonDirectory, string airframe, bool clearList = true)
        {
            if (airframe == "KEYEMULATOR" || airframe == "KEYEMULATOR_SRS" || airframe == "NOFRAMELOADEDYET")
            {
                return;
            }

            if (clearList)
            {
                if (_dcsbiosControls.Count > 0 && !_airFrameChanged)
                {
                    return;
                }
                _dcsbiosControls = new List<DCSBIOSControl>();
                _NS430Loaded = false;
            }

            try
            {
                lock (_lockObject)
                {
                    //Always read CommonData.json
                    var directoryInfo = new DirectoryInfo(jsonDirectory);
                    IEnumerable<FileInfo> files;
                    Common.DebugP("Searching for " + airframe + ".json in directory " + jsonDirectory);
                    try
                    {
                        files = directoryInfo.EnumerateFiles(airframe + ".json", SearchOption.TopDirectoryOnly);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Failed to find DCS-BIOS files. -> " + Environment.NewLine + ex.Message);
                    }
                    foreach (var file in files)
                    {
                        Common.DebugP("Opening " + file.DirectoryName + "\\" + file.Name);
                        var reader = file.OpenText();
                        string text;
                        try
                        {
                            text = reader.ReadToEnd();
                            //Debug.Print(text);
                        }
                        finally
                        {
                            reader.Close();
                        }

                        var jsonData = DCSBIOSJsonFormatterVersion1.Format(text);
                        //Debug.Print("\n--------------------------\n" + jsonData);
                        /*var newfile = File.CreateText(@"e:\temp\regexp_debug_output.txt.txt");
                        newfile.Write(jsonData);
                        newfile.Close();*/
                        var dcsBiosControlList = JsonConvert.DeserializeObject<DCSBIOSControlRootObject>(jsonData);
                        /*foreach (var control in dcsBiosControlList.DCSBIOSControls)
                        {
                            Debug.Print(control.description);
                        }*/
                        //Debug.Print("\n--------------------------\n" + jsonData);
                        _dcsbiosControls.AddRange(dcsBiosControlList.DCSBIOSControls);
                        PrintDuplicateControlIdentifiers(dcsBiosControlList.DCSBIOSControls);
                        /*foreach (var control in _dcsbiosControls)
                        {
                            Debug.Print(control.identifier);
                        }*/
                    }
                    var commonDataText = File.ReadAllText(jsonDirectory + "\\CommonData.json");
                    var commonDataControlsText = DCSBIOSJsonFormatterVersion1.Format(commonDataText);
                    var commonDataControls = JsonConvert.DeserializeObject<DCSBIOSControlRootObject>(commonDataControlsText);
                    _dcsbiosControls.AddRange(commonDataControls.DCSBIOSControls);


                    var metaDataEndText = File.ReadAllText(jsonDirectory + "\\MetadataEnd.json");
                    var metaDataEndControlsText = DCSBIOSJsonFormatterVersion1.Format(metaDataEndText);
                    var metaDataEndControls = JsonConvert.DeserializeObject<DCSBIOSControlRootObject>(metaDataEndControlsText);
                    _dcsbiosControls.AddRange(metaDataEndControls.DCSBIOSControls);
                    _airFrameChanged = false;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("LoadControls() : " + Environment.NewLine + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        public static void LoadControls()
        {
            LoadControls(_jsonDirectory, _airframe);
            if (UseNS430 && !_NS430Loaded)
            {
                LoadControls(_jsonDirectory, "NS430", false);
                
                _dcsbiosControls.Remove(_dcsbiosControls.FindLast(controlObject => controlObject.identifier.Equals("_UPDATE_COUNTER")));
                _dcsbiosControls.Remove(_dcsbiosControls.FindLast(controlObject => controlObject.identifier.Equals("_UPDATE_SKIP_COUNTER")));
                _NS430Loaded = true;
            }
        }

        public static DCSAirframe Airframe
        {
            get { return _airframe; }
            set
            {
                if (_airframe != value)
                {
                    _airframe = value;
                    _airFrameChanged = true;
                }
            }
        }

        public static string JSONDirectory
        {
            get { return _jsonDirectory; }
            set { _jsonDirectory = DBCommon.GetDCSBIOSJSONDirectory(value); }
        }

        public static bool UseNS430
        {
            get => _useNS430;
            set { _useNS430 = value;
                _dcsbiosControls.Clear();
            }
        }

        public static IEnumerable<DCSBIOSControl> GetControls()
        {
            LoadControls();
            return _dcsbiosControls;
        }

        public static IEnumerable<DCSBIOSControl> GetIntegerOutputControls()
        {
            if (_airframe == DCSAirframe.KEYEMULATOR || _airframe == DCSAirframe.KEYEMULATOR_SRS)
            {
                return null;
            }
            LoadControls();
            return _dcsbiosControls.Where(controlObject => (controlObject.outputs.Count > 0) && controlObject.outputs[0].OutputDataType == DCSBiosOutputType.INTEGER_TYPE);
        }

        public static IEnumerable<DCSBIOSControl> GetInputControls()
        {
            if (_airframe == DCSAirframe.KEYEMULATOR || _airframe == DCSAirframe.KEYEMULATOR_SRS)
            {
                return null;
            }
            LoadControls();
            return _dcsbiosControls.Where(controlObject => (controlObject.inputs.Count > 0));
        }
    }
}
