using System;
using System.Collections.Generic;
using System.Text;

namespace Levrum.DataBridge
{
    public enum ScriptType { PostLoad, PerIncident, FinalProcessing };

    public static class ScriptTemplates
    {
        public static Dictionary<ScriptType, Dictionary<string, string>> TemplateDictionary { get; set; }

        static ScriptTemplates()
        {
            try
            {
                TemplateDictionary = new Dictionary<ScriptType, Dictionary<string, string>>();
                TemplateDictionary.Add(ScriptType.PostLoad, new Dictionary<string, string>());
                TemplateDictionary.Add(ScriptType.PerIncident, new Dictionary<string, string>());
                TemplateDictionary.Add(ScriptType.FinalProcessing, new Dictionary<string, string>());
                foreach (KeyValuePair<string, string> kvp in postLoadScripts)
                {
                    TemplateDictionary[ScriptType.PostLoad].Add(kvp.Key, kvp.Value);
                }
                foreach (KeyValuePair<string, string> kvp in perIncidentScripts)
                {
                    TemplateDictionary[ScriptType.PerIncident].Add(kvp.Key, kvp.Value);
                }
                foreach (KeyValuePair<string, string> kvp in finalProcessingScripts)
                {
                    TemplateDictionary[ScriptType.FinalProcessing].Add(kvp.Key, kvp.Value);
                }
            } catch (Exception ex)
            {
                Levrum.Utils.LogHelper.LogException(ex, "Unable to generate script templates", true);
            }
        }

        #region Generic Scripts

        const string c_genericPerIncidentScript =
@"let numIncidents = Incidents.Count;

for (let i = 0; i < numIncidents; i++) {
  let incident = Incidents[i];

  if (i % 1000 === 0) {
    Engine.CollectGarbage(true);
    MapLoader.CollectNativeGarbage();
    MapLoader.UpdateJSProgress(`Processed ${i} out of ${numIncidents} incidents`, (i / numIncidents) * 100);
  }
}";

        const string c_genericPerResponseScript =
@"let numIncidents = Incidents.Count;

for (let i = 0; i < numIncidents; i++) {
  let incident = Incidents[i];
  let responses = incident.Responses;
  let numResponses = responses.Count;
  for (let k = 0; k < numResponses; k++) {
    let response = responses[k];

  }

  if (i % 1000 === 0) {
    Engine.CollectGarbage(true);
    MapLoader.CollectNativeGarbage();
    MapLoader.UpdateJSProgress(`Processed ${i} out of ${numIncidents} incidents`, (i / numIncidents) * 100);
  }

}";

        const string c_genericPerTimingDataScript =
@"let numIncidents = Incidents.Count;

for (let i = 0; i < numIncidents; i++) {
  let incident = Incidents[i];
  let responses = incident.Responses;
  let numResponses = responses.Count;
  for (let k = 0; k < numResponses; k++) {
    let response = responses[k];
    
    let timings = response.TimingData;
    let numTimings = timings.Count;
    for (let l = 0; l < numTimings; l++) {
      let timing = timings[l];
    }
  }

  if (i % 1000 === 0) {
    Engine.CollectGarbage(true);
    MapLoader.CollectNativeGarbage();
    MapLoader.UpdateJSProgress(`Processed ${i} out of ${numIncidents} incidents`, (i / numIncidents) * 100);
  }
}";
        const string c_genericPerTimingDataWithTipsScript =
@"let numIncidents = Incidents.Count;

for (let i = 0; i < numIncidents; i++) {
  let incident = Incidents[i];
  let responses = incident.Responses;
  let numResponses = responses.Count;
  for (let k = 0; k < numResponses; k++) {
    let response = responses[k];
    // let clearScene = response.GetTimingDataByName(""ClearScene""); // Much faster than iterating!

    // Or you can iterate :D
    let timings = response.TimingData;
    let numTimings = timings.Count;
    for (let l = 0; l < numTimings; l++) {
      let timing = timings[l];
      // Add your code to modify the incident, response, or piece of timing data here.
    }
  }

  // Run the garbage collector every 1000 incidents to keep things fast
  if (i % 1000 === 0) {
    Engine.CollectGarbage(true);
    MapLoader.CollectNativeGarbage();
    MapLoader.UpdateJSProgress(`Processed ${i} out of ${numIncidents} incidents`, (i / numIncidents) * 100);
  }
}";
        #endregion

        #region Post-Loading Scripts

        static KeyValuePair<string, string> postLoadingPerIncidentKVP = new KeyValuePair<string, string>("Per Incident", c_genericPerIncidentScript);
        static KeyValuePair<string, string> postLoadingPerResponseKVP = new KeyValuePair<string, string>("Per Response", c_genericPerResponseScript);
        static KeyValuePair<string, string> postLoadingPerTimingDataScriptKVP = new KeyValuePair<string, string>("Per Response Timing", c_genericPerTimingDataScript);
        static KeyValuePair<string, string> postLoadingPerTimingDataWithTipsKVP = new KeyValuePair<string, string>("Per Response Timing (with tips)", c_genericPerTimingDataWithTipsScript);

        #endregion

        #region Per Incident Scripts
        static KeyValuePair<string, string> perIncidentScriptKVP = new KeyValuePair<string, string>("Per Incident", c_perIncidentScript);
        static KeyValuePair<string, string> perIncidentScriptWithTipsKVP = new KeyValuePair<string, string>("Per Incident (with tips)", c_perIncidentWithTipsScript);
        static KeyValuePair<string, string> perIncidentPerResponseScriptKVP = new KeyValuePair<string, string>("Per Response", c_perIncidentPerResponseScript);
        static KeyValuePair<string, string> perIncidentPerResponseWithTipsKVP = new KeyValuePair<string, string>("Per Response (with tips)", c_perIncidentPerResponseWithTipsScript);
        static KeyValuePair<string, string> perIncidentPerTimingDataScriptKVP = new KeyValuePair<string, string>("Per Response Timing", c_perIncidentPerTimingDataScript);
        static KeyValuePair<string, string> perIncidentPerTimingDataWithTipsKVP = new KeyValuePair<string, string>("Per Response Timing (with tips)", c_perIncidentPerTimingDataWithTipsScript);
        static KeyValuePair<string, string> perIncidentSetCancellationFlagKVP = new KeyValuePair<string, string>("Set Cancelled if OnScene is null", c_perIncidentSetCancellationScript);
        static KeyValuePair<string, string> perIncidentSetShiftKVP = new KeyValuePair<string, string>("Set Shift based on JavaScript table", c_perIncidentSetShiftScript);
        const string c_perIncidentScript =
@"if (script === undefined) {
  var script = () => {
    // Write your script here!
  }
}

script();
";

        const string c_perIncidentWithTipsScript =
@"if (script === undefined) {
  // Only define the script one time
  var script = () => {
    // Get an IncidentData value you need:
    // let time = Incident.GetDataValue(""Time"");
    // Or get more than one much more quickly! 
    // GetDataValues only takes as long as one GetDataValue no matter how much data you retrieve
    // let dataKeys = XHost.newArr(string, 2)
    // dataKeys[0] = ""Time"";
    // dataKeys[1] = ""Code"";
    // let dataValues = Incident.GetDataValues(dataKeys);

    // Modify the Incident values here;
    // let updatedKeys = XHost.newArr(string, 2);
    // let updatedValues = XHost.newArr(string, 2);
    // let updatedKeys[0] = ""Shift"";
    // let updatedValues[0] = ""A"";
    // let updatedKeys[1] = ""Code"";
    // let updatedValues[1] = ""New Code"";
    // Incident.SetDataValues(updatedKeys, updatedValues);
  }
}

// Run the script once it's defined
script();";

        const string c_perIncidentPerResponseScript =
@"if (script === undefined) {
  var script = () => {
    let responses = Incident.Responses;
    let numResponses = responses.Count;
    for (let i = 0; i < numResponses; i++) {
      let response = responses[i];
    }
  }
}

script();
";

        const string c_perIncidentPerResponseWithTipsScript =
@"if (script === undefined) {
  // Only define the script one time
  var script = () => {
    // Get an IncidentData value you need:
    // let time = Incident.GetDataValue(""Time"");
    // Or get more than one much more quickly! 
    // GetDataValues only takes as long as one GetDataValue no matter how much data you retrieve
    // let dataKeys = XHost.newArr(string, 2)
    // dataKeys[0] = ""Time"";
    // dataKeys[1] = ""Code"";
    // let dataValues = Incident.GetDataValues(dataKeys);
    
    let responses = Incident.Responses;
    let numResponses = responses.Count;
    for (let i = 0; i < numResponses; i++) {
      let response = responses[i];
      
      // Get two data values from the response
      // let responseKeys = XHost.newArr(string, 2)
      // responseKeys[0] = ""Unit"";
      // responseKeys[1] = ""UnitType"";
      // let responseValues = response.GetDataValues(responseKeys);

      // if (responseValues[0] == ""Medic 1"") {
        // Modify the Incident or Response based on the values here;
        // let updatedKeys = XHost.newArr(string, 2);
        // let updatedValues = XHost.newArr(string, 2);
        // let updatedKeys[0] = ""Unit"";
        // let updatedValues[0] = ""MEDIC1"";
        // let updatedKeys[1] = ""UnitType"";
        // let updatedValues[1] = ""Medic"";
        // response.SetDataValues(updatedKeys, updatedValues);
      // }
    }
  }
}

// Run the script once it's defined
script();";

        const string c_perIncidentPerTimingDataScript =
@"if (script === undefined) {
  var script = () => {
    let responses = Incident.Responses;
    let numResponses = responses.Count;

    for (let i = 0; i < numResponses; i++) {
      let response = responses[i];

      let timings = response.TimingData;
      let numTimings = timings.Count;
      for (let k = 0; k < numTimings; k++) {
        let timing = timings[k];
      }
    }
  }
}

script();";

        const string c_perIncidentPerTimingDataWithTipsScript =
@"if (script === undefined) {
  // Only define the script one time
  var script = () => {
    // Get an IncidentData value you need:
    // let time = Incident.GetDataValue(""Time"");

    let responses = Incident.Responses;
    let numResponses = responses.Count;
    for (let i = 0; i < numResponses; i++) {
      let response = responses[i];
      // Get two data values from the response
      // GetDataValues only takes as long as one GetDataValue no matter how much data you retrieve
      // let responseKeys = XHost.newArr(string, 2)
      // responseKeys[0] = ""Unit"";
      // responseKeys[1] = ""UnitType"";
      // let responseValues = response.GetDataValues(responseKeys);

      // Check to see if the time between assignment and arrival exceeds a threshold for a medics and flag the incident
      // let assigned = response.GetTimingDataByName(""Assigned"");
      // let onScene = response.GetTimingDataByName(""OnScene"");
      // if (responseValues[1].toUpperCase() == ""MEDIC"" && (onScene.Value - assigned.Value) > 15) {
        // Incident.SetDataValue(""SlowMedic"", true);
      // }

      // Or you can iterate through each piece of TimingData, though this is slower
      let timings = response.TimingData;
      let numTimings = timings.Count;
      for (let k = 0; k < numTimings; k++) {
        let timing = timings[k];
        // Modify the Incident or Response based on the TimingData here;
      }
    }
  }
}

// Run the script once it's defined
script();";

        const string c_perIncidentSetCancellationScript =
@"if (script === undefined) {
  var script = () => {
    let setCancelled = true;
    let responses = Incident.Responses;
    let numResponses = responses.Count;
    for (let i = 0; i < numResponses; i++) {
      let response = responses[i];
      let onScene = response.GetTimingDataByName(""OnScene"");
      if (onScene !== undefined && onScene !== null) {
        let onSceneValue = onScene.Value;
        if (onSceneValue > 0) {
          // At least one responding unit arrived on scene, do not set cancelled;
          setCancelled = false;
        }
      }
    }

    if (setCancelled === true) {
      Incident.SetDataValue(""Cancelled"", true);
    }
  }
}

script();";

        const string c_perIncidentSetShiftScript =
@"if (script === undefined) {
  var aShift = {
    0: ""C"", // Hour of day starting at 0000
    1: ""C"",
    2: ""C"",
    3: ""C"",
    4: ""C"",
    5: ""C"",
    6: ""C"",
    7: ""C"",
    8: ""A"",
    9: ""A"",
    10: ""A"",
    11: ""A"",
    12: ""A"",
    13: ""A"",
    14: ""A"",
    15: ""A"",
    16: ""A"",
    17: ""A"",
    18: ""A"",
    19: ""A"",
    20: ""A"",
    21: ""A"",
    22: ""A"",
    23: ""A"",
  };

  var bShift = {
    0: ""A"",
    1: ""A"",
    2: ""A"",
    3: ""A"",
    4: ""A"",
    5: ""A"",
    6: ""A"",
    7: ""A"",
    8: ""B"",
    9: ""B"",
    10: ""B"",
    11: ""B"",
    12: ""B"",
    13: ""B"",
    14: ""B"",
    15: ""B"",
    16: ""B"",
    17: ""B"",
    18: ""B"",
    19: ""B"",
    20: ""B"",
    21: ""B"",
    22: ""B"",
    23: ""B"",
  };

  var cShift = {
    0: ""B"",
    1: ""B"",
    2: ""B"",
    3: ""B"",
    4: ""B"",
    5: ""B"",
    6: ""B"",
    7: ""B"",
    8: ""C"",
    9: ""C"",
    10: ""C"",
    11: ""C"",
    12: ""C"",
    13: ""C"",
    14: ""C"",
    15: ""C"",
    16: ""C"",
    17: ""C"",
    18: ""C"",
    19: ""C"",
    20: ""C"",
    21: ""C"",
    22: ""C"",
    23: ""C"",
  };

  var shiftTable = {
    2020: {
      // Year
      1: {
        // Month
        1: aShift, // Day of Month
        2: bShift,
        3: cShift,
        4: aShift,
        5: bShift,
        6: cShift,
        7: aShift,
        8: bShift,
        9: cShift,
        10: aShift,
        11: bShift,
        12: cShift,
        13: aShift,
        14: bShift,
        15: cShift,
        16: aShift,
        17: bShift,
        18: cShift,
        19: aShift,
        20: bShift,
        21: cShift,
        22: aShift,
        23: bShift,
        24: cShift,
        25: aShift,
        26: bShift,
        27: cShift,
        28: aShift,
        29: bShift,
        30: cShift,
        31: aShift,
      },
    },
  };

  var script = () => {
    let timeComponents = Incident.GetDataDateTimeComponents(""Time"");
    let year = timeComponents[0];
    let month = timeComponents[1];
    let day = timeComponents[2];
    let hour = timeComponents[3];
    let shiftYearTable = shiftTable[year];
    let shift = null;
    if (shiftYearTable !== undefined) {
      let shiftMonthTable = shiftYearTable[month];
      if (shiftMonthTable !== undefined) {
        let shiftDayTable = shiftMonthTable[day];
        if (shiftDayTable !== undefined) {
          shift = shiftDayTable[hour];
        }
      }
    }

    if (shift !== null) {
      Incident.SetDataValue(""Shift"", shift);
    }
  };
}

script();";
        #endregion

        #region Final Processing Scripts
        static KeyValuePair<string, string> finalProcessingPerIncidentKVP = new KeyValuePair<string, string>("Per Incident", c_genericPerIncidentScript);
        static KeyValuePair<string, string> finalProcessingPerResponseKVP = new KeyValuePair<string, string>("Per Response", c_genericPerResponseScript);
        static KeyValuePair<string, string> finalProcessingPerTimingDataScriptKVP = new KeyValuePair<string, string>("Per Response Timing", c_genericPerTimingDataScript);
        static KeyValuePair<string, string> finalProcessingPerTimingDataWithTipsKVP = new KeyValuePair<string, string>("Per Response Timing (with tips)", c_genericPerTimingDataWithTipsScript);

        #endregion

        static KeyValuePair<string, string>[] postLoadScripts = new KeyValuePair<string, string>[] { postLoadingPerIncidentKVP, postLoadingPerResponseKVP, postLoadingPerTimingDataScriptKVP, postLoadingPerTimingDataWithTipsKVP };
        static KeyValuePair<string, string>[] perIncidentScripts = new KeyValuePair<string, string>[] { perIncidentScriptKVP, perIncidentScriptWithTipsKVP, perIncidentPerResponseScriptKVP, perIncidentPerResponseWithTipsKVP, perIncidentPerTimingDataScriptKVP, perIncidentPerTimingDataWithTipsKVP, perIncidentSetCancellationFlagKVP, perIncidentSetShiftKVP };
        static KeyValuePair<string, string>[] finalProcessingScripts = new KeyValuePair<string, string>[] { finalProcessingPerIncidentKVP, finalProcessingPerResponseKVP, finalProcessingPerTimingDataScriptKVP, finalProcessingPerTimingDataWithTipsKVP };
    }


}
