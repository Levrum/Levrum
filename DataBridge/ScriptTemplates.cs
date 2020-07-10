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
        const string c_genericPerTimingDataScript =
@"let numIncidents = Incidents.Count;
let incident,
  responses,
  numResponses,
  response,
  timings,
  numTimings,
  timing;

// Define any variables you will use inside the loops here to avoid garbage collection
for (let i = 0; i < numIncidents; i++) {
  incident = Incidents[i];
  responses = incident.Responses;
  numResponses = responses.Count;
  for (let k = 0; k < numResponses; k++) {
    response = responses[k];
    timings = response.TimingData;
    numTimings = timings.Count;
    for (let l = 0; l < numTimings; l++) {
      timing = timings[l];
      // Add your code to modify the incident, response, or piece of timing data here.
    }
  }

  // Run the garbage collector every 500 incidents
  if (i % 500 === 0) {
    Engine.CollectGarbage(true);
  }
}";
        #endregion

        #region Post-Loading Scripts
        static KeyValuePair<string, string> postLoadingPerTimingDataScriptKVP = new KeyValuePair<string, string>("Per Response Timing", c_genericPerTimingDataScript);
        #endregion

        #region Per Incident Scripts
        static KeyValuePair<string, string> perIncidentScriptKVP = new KeyValuePair<string, string>("Per Incident", c_perIncidentScript);
        static KeyValuePair<string, string> perIncidentPerResponseScriptKVP = new KeyValuePair<string, string>("Per Response", c_perIncidentPerResponseScript);
        static KeyValuePair<string, string> perIncidentPerTimingDataScriptKVP = new KeyValuePair<string, string>("Per Response Timing", c_perIncidentPerTimingDataScript);

        const string c_perIncidentScript =
@"if (script === undefined) {
  // Only define the script one time
  var script = () => {
    // Define any variables you need here as in the following example:
    // let time = Incident.GetDataValue(""Time"");
    
    // For garbage collection, if you add variables set them to undefined here
    // time = undefined;
  }
}

// Run the script once it's defined
script();";

        const string c_perIncidentPerResponseScript =
@"if (script === undefined) {
  // Only define the script one time
  var script = () => {
    let responses, numResponses, response;
    // Add any variables you need inside the loop here.
    
    responses = Incident.Responses;
    numResponses = responses.Count;
    for (let i = 0; i < numResponses; i++) {
      response = responses[i];
      // Modify the Incident or Response here;
    }

    // For garbage collection, if you add variables set them to undefined here
    responses = undefined;
    numResponses = undefined;
    response = undefined;
  }
}

// Run the script once it's defined
script();";

        const string c_perIncidentPerTimingDataScript =
@"if (script === undefined) {
  // Only define the script one time
  var script = () => {
    let responses, numResponses, response, timings, numTimings, timing;
    // Add any variables you need inside the loops here.
    
    responses = Incident.Responses;
    numResponses = responses.Count;
    for (let i = 0; i < numResponses; i++) {
      response = responses[i];
      timings = response.TimingData;
      numTimings = timings.Count;
      for (let k = 0; k < numTimings; k++) {
        timing = timings[k];
        // Modify the Incident or Response based on the TimingData here;
      }
    }

    // For garbage collection, if you add variables set them to undefined here
    responses = undefined;
    numResponses = undefined;
    response = undefined;
    timings = undefined;
    numTimings = undefined;
    timing = undefined;
  }
}

// Run the script once it's defined
script();";
        #endregion

        #region Final Processing Scripts
        static KeyValuePair<string, string> finalProcessingPerTimingDataScriptKVP = new KeyValuePair<string, string>("Per Response Timing", c_genericPerTimingDataScript);

        #endregion

        static KeyValuePair<string, string>[] postLoadScripts = new KeyValuePair<string, string>[] { postLoadingPerTimingDataScriptKVP };
        static KeyValuePair<string, string>[] perIncidentScripts = new KeyValuePair<string, string>[] { perIncidentPerResponseScriptKVP, perIncidentPerTimingDataScriptKVP };
        static KeyValuePair<string, string>[] finalProcessingScripts = new KeyValuePair<string, string>[] { finalProcessingPerTimingDataScriptKVP };
    }


}
