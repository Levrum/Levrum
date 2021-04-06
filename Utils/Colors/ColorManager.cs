using Levrum.Utils.Infra;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Levrum.Utils.Colors
{
    public class ColorManager
    {
        /*      "Tutorial"
         *      
         *          Saving Colors
         *      Create a dicionary of ColorableObjects with the name of the object as the key.
         *      Each object will act as a link between any named object and the colors for that object.
         *      
         *      Create a dictionary for each category of object. For example, "Stations", "Regions", "Causes", etc.
         *      That dictionary can be passed into SaveColorCSV() to save the initial dictionary.
         *      Alternatively, AddCategory() and UpdateCategory() can be used to edit the dictionary one category at a time.
         * 
         *          Loading Colors
         *      You can get the colors you want by simply passing in a list of the name of objects you want colors for.
         *      Get_____ColorLookup() will return a dictionary with the keys as values and the colors as the values
         *      GetCategory() will return the category of the colors you passed in.
         *      LoadColorCSV() will load the full dictionary while LoadCategoryByLookup() and LoadCategoryByName() will return a single category
         */

        /// <summary>
        /// This is the access point for actually accessing the custom colors. Just pass it a list of objects names you want colors for.
        /// It will return a dictionary with those object names as the key and the cooresponding colorable object's color
        /// </summary>
        /// <param name="lookupValues"></param>
        /// <returns>Dictionary<string, Color></returns>
        public Dictionary<string, Color> GetFillColorLookup(List<string> lookupValues)
        {
            string fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                return BestMatchMethod(lookupValues, LoadColorCSV(), LoadManyToOneLookupDict());
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return new Dictionary<string, Color>();
            }
        }

        public Dictionary<string, Color> GetBorderColorLookup(List<string> lookupValues)
        {
            string fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                return BestMatchMethod(lookupValues, LoadColorCSV(), LoadManyToOneLookupDict());
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return new Dictionary<string, Color>();
            }
        }

        private Dictionary<string, Color> BestMatchMethod(List<string> lookupValues, Dictionary<string, Dictionary<string, ColorableObject>> customColorDict, Dictionary<string, string> manyToOneLookup = null, bool getBorderColor = false)
        {
            //This finds the best matching category based on how well the lookupValues match up with each custom color category
            //It will work for any list of strings that match up with the names of the ColorableObjects
            //What is returned is a dictionary with the lookup value as the key and the cooresponding color as the value

            //manyToOneLookup must have values that match the names of items in the colorableColorDict. For example, C3S results as the key and the model as the value
            if (customColorDict == null || customColorDict.Count == 0)
            {
                return new Dictionary<string, Color>();
            }

            customColorDict["Many to One Custom Key"] = new Dictionary<string, ColorableObject>();
            if (manyToOneLookup != null)
            {
                foreach (string manyToOneLookupKey in manyToOneLookup.Keys)
                {
                    customColorDict["Many to One Custom Key"][manyToOneLookupKey] = new ColorableObject(manyToOneLookupKey, "Unknown");
                }
            }

            Dictionary<string, Color> colorLookupDict = new Dictionary<string, Color>();
            float bestMatchPercent = 0;
            int bestFuzzyMatchCount = 0;
            int bestExactMatchCount = 0;
            string bestMatchCat = "";

            foreach (var cat in customColorDict)
            {
                int exactMatchCount = 0;
                int fuzzyMatchCount = 0;
                foreach (ColorableObject obj in cat.Value.Values)
                {
                    if (lookupValues.Contains(obj.Name))
                    {
                        fuzzyMatchCount++;
                    }
                    foreach (string lookupValue in lookupValues)
                    {
                        if (lookupValue == obj.Name)
                        {
                            exactMatchCount++;
                        }
                    }
                }

                if (exactMatchCount > bestExactMatchCount)
                {
                    //The best category to choose is where the most fields match exactly what we were given
                    bestExactMatchCount = exactMatchCount;
                    bestFuzzyMatchCount = fuzzyMatchCount;
                    bestMatchPercent = (exactMatchCount + fuzzyMatchCount) / (cat.Value.Count / 2);
                    bestMatchCat = cat.Key;
                }
                else if (exactMatchCount == bestExactMatchCount && fuzzyMatchCount > bestFuzzyMatchCount)
                {
                    //If there is an exact match tie, go with the one with more fuzzy matches
                    bestFuzzyMatchCount = fuzzyMatchCount;
                    bestMatchPercent = (exactMatchCount + fuzzyMatchCount) / (cat.Value.Count / 2);
                    bestMatchCat = cat.Key;
                }
                else if (exactMatchCount == bestExactMatchCount && fuzzyMatchCount == bestFuzzyMatchCount &&
                    (Convert.ToDouble(fuzzyMatchCount) + Convert.ToDouble(exactMatchCount))
                    / (Convert.ToDouble(cat.Value.Count) * 2) > bestMatchPercent)
                {
                    //In the case of a tie with both match types, go with the one with the highest % of matches
                    bestMatchPercent = fuzzyMatchCount / cat.Value.Count;
                    bestMatchCat = cat.Key;
                }
            }

            if (bestMatchCat != "")
            {
                if (bestMatchCat != "Many to One Custom Key")
                {
                    foreach (string lookupValue in lookupValues)
                    {
                        if (customColorDict[bestMatchCat].ContainsKey(lookupValue))
                        {
                            ColorableObject obj = customColorDict[bestMatchCat][lookupValue];
                            if (!getBorderColor)
                            {
                                if (obj.FillColor != Color.Empty)
                                {
                                    colorLookupDict[lookupValue] = customColorDict[bestMatchCat][lookupValue].FillColor;
                                }
                                else if (!string.IsNullOrEmpty(obj.Parent) && customColorDict.ContainsKey(obj.ParentType) &&
                                    customColorDict[obj.ParentType].ContainsKey(obj.Parent) && customColorDict[obj.ParentType][obj.Parent].FillColor != Color.Empty)
                                {
                                    //Wow that is a mess of an if....lol
                                    //If it didn't have a color itself but it's parent did, use the parent's color...
                                    colorLookupDict[lookupValue] = customColorDict[obj.ParentType][obj.Parent].FillColor;
                                }
                            }
                            else
                            {
                                if (obj.BorderColor != Color.Empty)
                                {
                                    colorLookupDict[lookupValue] = customColorDict[bestMatchCat][lookupValue].BorderColor;
                                }
                                else if (!string.IsNullOrEmpty(obj.Parent) && customColorDict.ContainsKey(obj.ParentType) &&
                                    customColorDict[obj.ParentType].ContainsKey(obj.Parent) && customColorDict[obj.ParentType][obj.Parent].BorderColor != Color.Empty)
                                {
                                    //Wow that is a mess of an if....lol
                                    //If it didn't have a color itself but it's parent did, use the parent's color...
                                    colorLookupDict[lookupValue] = customColorDict[obj.ParentType][obj.Parent].BorderColor;
                                }
                            }

                        }
                    }
                }
                else
                {
                    //Getting recursive up in here!
                    Dictionary<string, Color> tempColorLookupDict = BestMatchMethod(manyToOneLookup.Values.ToList(), customColorDict);
                    foreach (var lookupValue in lookupValues)
                    {
                        if (manyToOneLookup.ContainsKey(lookupValue) && tempColorLookupDict.ContainsKey(manyToOneLookup[lookupValue]))
                        {
                            //A bit confusing but many to one uses the values we were passed to get the corresponding colorable object
                            colorLookupDict[lookupValue] = tempColorLookupDict[manyToOneLookup[lookupValue]];
                        }
                    }
                }
            }

            return colorLookupDict;
        }

        public string GetColorCategory(List<string> lookupValues)
        {
            string fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                return BestMatchMethod_ReturnCategory(lookupValues, LoadColorCSV(), LoadManyToOneLookupDict());
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return null;
            }


        }

        private string BestMatchMethod_ReturnCategory(List<string> lookupValues, Dictionary<string, Dictionary<string, ColorableObject>> customColorDict, Dictionary<string, string> manyToOneLookup = null)
        {
            //This is a copy of the best match method but it will return the category instead of the color lookup...
            if (customColorDict == null || customColorDict.Count == 0)
            {
                return null;
            }

            customColorDict["Many to One Custom Key"] = new Dictionary<string, ColorableObject>();
            if (manyToOneLookup != null)
            {
                foreach (string manyToOneLookupKey in manyToOneLookup.Keys)
                {
                    customColorDict["Many to One Custom Key"][manyToOneLookupKey] = new ColorableObject(manyToOneLookupKey, "Unknown");
                }
            }

            float bestMatchPercent = 0;
            int bestFuzzyMatchCount = 0;
            int bestExactMatchCount = 0;
            string bestMatchCat = "";

            foreach (var cat in customColorDict)
            {
                int exactMatchCount = 0;
                int fuzzyMatchCount = 0;
                foreach (ColorableObject obj in cat.Value.Values)
                {
                    if (lookupValues.Contains(obj.Name))
                    {
                        fuzzyMatchCount++;
                    }
                    foreach (string lookupValue in lookupValues)
                    {
                        if (lookupValue == obj.Name)
                        {
                            exactMatchCount++;
                        }
                    }
                }

                if (exactMatchCount > bestExactMatchCount)
                {
                    //The best category to choose is where the most fields match exactly what we were given
                    bestExactMatchCount = exactMatchCount;
                    bestFuzzyMatchCount = fuzzyMatchCount;
                    bestMatchPercent = (exactMatchCount + fuzzyMatchCount) / (cat.Value.Count / 2);
                    bestMatchCat = cat.Key;
                }
                else if (exactMatchCount == bestExactMatchCount && fuzzyMatchCount > bestFuzzyMatchCount)
                {
                    //If there is an exact match tie, go with the one with more fuzzy matches
                    bestFuzzyMatchCount = fuzzyMatchCount;
                    bestMatchPercent = (exactMatchCount + fuzzyMatchCount) / (cat.Value.Count / 2);
                    bestMatchCat = cat.Key;
                }
                else if (exactMatchCount == bestExactMatchCount && fuzzyMatchCount == bestFuzzyMatchCount &&
                    (Convert.ToDouble(fuzzyMatchCount) + Convert.ToDouble(exactMatchCount))
                    / (Convert.ToDouble(cat.Value.Count) * 2) > bestMatchPercent)
                {
                    //In the case of a tie with both match types, go with the one with the highest % of matches
                    bestMatchPercent = fuzzyMatchCount / cat.Value.Count;
                    bestMatchCat = cat.Key;
                }
            }

            if (bestMatchCat != "")
            {
                if (bestMatchCat != "Many to One Custom Key")
                {
                    return bestMatchCat;
                }
                else
                {
                    //Getting recursive up in here!
                    return BestMatchMethod_ReturnCategory(manyToOneLookup.Values.ToList(), customColorDict);
                }
            }
            return null;
        }

        /// <summary>
        /// Saving and loading of the custom color dictionaries.
        /// The 1st level dictionary's key is a category(e.g. "Models", "Causes", etc.)
        /// The second level key is the colorable object's name(e.g. "EMS", "Fire", etc.)
        /// </summary>
        /// <param name="colorableObjects"></param>
        public bool SaveColorCSV(Dictionary<string, Dictionary<string, ColorableObject>> colorableObjects)
        {
            string fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                Directory.CreateDirectory(AppSettings.ColorDir);

                colorableObjects = SortObjectsProperly(colorableObjects);

                string saveDestination = AppSettings.ColorDir + "CustomColors.csv";
                if (File.Exists(saveDestination + "CustomColors.csv"))
                {
                    //Prompt user to see if they want to overwrite existing file
                }

                using (StreamWriter sw = new StreamWriter(saveDestination))
                {
                    sw.WriteLine("\"Type\",\"Name\",\"TypeGrouping\",\"Parent\",\"ParentType\",\"A\",\"R\",\"G\",\"B\",\"A2\",\"R2\",\"G2\",\"B2\"");
                    foreach (var objCategory in colorableObjects.Values)
                    {
                        foreach (ColorableObject obj in objCategory.Values)
                        {
                            if (obj.FillColor != Color.Empty)
                            {
                                if (obj.BorderColor != Color.Empty)
                                {
                                    //Write all colors
                                    sw.WriteLine(string.Concat("\"", obj.Type, "\",\"", obj.Name, "\",\"", obj.TypeGrouping, "\",\"", obj.Parent, "\",\"", obj.ParentType, "\",\"",
                                        obj.FillColor.A, "\",\"", obj.FillColor.R, "\",\"", obj.FillColor.G, "\",\"", obj.FillColor.B, "\",\"",
                                        obj.BorderColor.A, "\",\"", obj.BorderColor.R, "\",\"", obj.BorderColor.G, "\",\"", obj.BorderColor.B, "\""));
                                }
                                else
                                {
                                    //Write fill color but no border
                                    sw.WriteLine(string.Concat("\"", obj.Type, "\",\"", obj.Name, "\",\"", obj.TypeGrouping, "\",\"", obj.Parent, "\",\"", obj.ParentType, "\",\"",
                                        obj.FillColor.A, "\",\"", obj.FillColor.R, "\",\"", obj.FillColor.G, "\",\"", obj.FillColor.B, "\",\"",
                                        "\",\"\",\"\",\"\",\"\""));
                                }
                            }
                            else
                            {
                                if (obj.BorderColor != Color.Empty)
                                {
                                    //Write border color but no fill
                                    sw.WriteLine(string.Concat("\"", obj.Type, "\",\"", obj.Name, "\",\"", obj.TypeGrouping, "\",\"", obj.Parent, "\",\"", obj.ParentType, "\",\"",
                                        "\",\"\",\"\",\"\",\"",
                                        obj.BorderColor.A, "\",\"", obj.BorderColor.R, "\",\"", obj.BorderColor.G, "\",\"", obj.BorderColor.B, "\""));
                                }
                                else
                                {
                                    //Write no color
                                    sw.WriteLine(string.Concat("\"", obj.Type, "\",\"", obj.Name, "\",\"", obj.TypeGrouping, "\",\"", obj.Parent, "\",\"", obj.ParentType, "\",\"",
                                        "\",\"\",\"\",\"\",\"",
                                        "\",\"\",\"\",\"\",\"\""));
                                }
                            }
                        }
                    }

                    sw.Close();
                }
                return true;
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return false;
            }
        }

        public bool UpdateCategory(Dictionary<string, ColorableObject> colorableCategory)
        {
            try
            {
                if (colorableCategory.Count == 0)
                {
                    return false;
                }

                string category = GetColorCategory(colorableCategory.Keys.ToList());
                if (string.IsNullOrEmpty(category))
                {
                    return false;
                }
                Dictionary<string, Dictionary<string, ColorableObject>> colorDictionary = LoadColorCSV();

                foreach (var obj in colorDictionary[category].ToArray())
                {
                    if (colorableCategory.ContainsKey(obj.Key) && colorDictionary[category][obj.Key].FillColor != colorableCategory[obj.Key].FillColor
                        && colorableCategory[obj.Key].FillColor != Color.Empty)
                    {
                        //If the fill color changed, add it
                        colorDictionary[category][obj.Key].FillColor = colorableCategory[obj.Key].FillColor;
                    }

                    if (colorableCategory.ContainsKey(obj.Key) && colorDictionary[category][obj.Key].BorderColor != colorableCategory[obj.Key].BorderColor
                        && colorableCategory[obj.Key].BorderColor != Color.Empty)
                    {
                        colorDictionary[category][obj.Key].BorderColor = colorableCategory[obj.Key].BorderColor;
                    }

                    colorableCategory.Remove(obj.Key);
                }

                foreach (var unmatchedObj in colorableCategory)
                {
                    if (!colorDictionary.ContainsKey(unmatchedObj.Key))
                    {
                        //No existing object in the dictionary. Should we add a new one?
                        //Probably...

                    }
                }

                SaveColorCSV(colorDictionary);
                return true;
            }
            catch
            {
                return false;
            }

        }

        public void AddCategory(string category, Dictionary<string, ColorableObject> colorableCategory)
        {
            //This will either add or overwrite any category with the new values.
            try
            {
                Dictionary<string, Dictionary<string, ColorableObject>> colorDictionary = LoadColorCSV();
                colorDictionary[category] = colorableCategory;
                SaveColorCSV(colorDictionary);
            }
            catch
            {

            }
        }

        public Dictionary<string, ColorableObject> LoadCategoryByLookup(List<string> lookupValues)
        {
            try
            {
                if (lookupValues.Count > 0)
                {
                    string category = GetColorCategory(lookupValues);
                    if (string.IsNullOrEmpty(category))
                    {
                        return new Dictionary<string, ColorableObject>();
                    }

                    Dictionary<string, Dictionary<string, ColorableObject>> colorDictionary = LoadColorCSV();

                    if (colorDictionary.ContainsKey(category))
                    {
                        return colorDictionary[category];
                    }
                    else
                    {
                        return new Dictionary<string, ColorableObject>();
                    }
                }
                else
                {
                    return new Dictionary<string, ColorableObject>();
                }
            }
            catch
            {
                return new Dictionary<string, ColorableObject>();
            }
        }

        public Dictionary<string, ColorableObject> LoadCategoryByName(string category)
        {
            try
            {
                Dictionary<string, Dictionary<string, ColorableObject>> colorDictionary = LoadColorCSV();
                if (colorDictionary.ContainsKey(category))
                {
                    return colorDictionary[category];
                }
                else
                {
                    return new Dictionary<string, ColorableObject>();
                }
            }
            catch
            {
                return new Dictionary<string, ColorableObject>();
            }
        }

        public Dictionary<string, Dictionary<string, ColorableObject>> LoadColorCSV()
        {
            string customColorSource = AppSettings.ColorDir + "CustomColors.csv";
            string fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                Dictionary<string, Dictionary<string, ColorableObject>> colorableObjects = new Dictionary<string, Dictionary<string, ColorableObject>>();
                if (File.Exists(customColorSource))
                {
                    using (StreamReader sr = new StreamReader(customColorSource))
                    {
                        string line = sr.ReadLine(); //header
                        if (line != "\"Type\",\"Name\",\"TypeGrouping\",\"Parent\",\"ParentType\",\"A\",\"R\",\"G\",\"B\",\"A2\",\"R2\",\"G2\",\"B2\"")
                        {
                            //Header isn't right...stopping now to prevent problems
                            return colorableObjects;
                        }
                        string[] header = line.Split(',');

                        int fillAlphaIndex = 0;
                        int borderAlphaIndex = 0;
                        int i = 0;
                        foreach (string s in header)
                        {
                            if (s == "\"A\"")
                            {
                                fillAlphaIndex = i;
                            }
                            if (s == "\"A2\"")
                            {
                                borderAlphaIndex = i;
                            }
                            i++;
                        }

                        line = sr.ReadLine();
                        while (line != null)
                        {
                            string[] values = line.Split(',');
                            List<string> valueCorrection = new List<string>();
                            string appendedValue = "";
                            foreach (string value in values)
                            {
                                if (value.Length < 2)
                                {
                                    if (!string.IsNullOrEmpty(value) && value != "\"")
                                    {
                                        appendedValue += ",";
                                        appendedValue += value;
                                        continue;
                                    }
                                    else if (!string.IsNullOrEmpty(appendedValue) && value == "\"")
                                    {
                                        appendedValue += ",";
                                        valueCorrection.Add(appendedValue);
                                        appendedValue = "";
                                        continue;
                                    }
                                    else
                                    {
                                        //I believe this shouldn't get hit but if the value is null, don't worry about it...
                                        continue;
                                    }
                                }

                                if (value.First() == '"' && value.Last() == '"')
                                {
                                    valueCorrection.Add(value.Substring(1, value.Length - 2));
                                }
                                else if (value.First() == '"')
                                {
                                    appendedValue = value.Substring(1, value.Length - 1);
                                }
                                else if (value.Last() == '"')
                                {
                                    appendedValue += ",";
                                    appendedValue += value.Substring(0, value.Length - 1);
                                    valueCorrection.Add(appendedValue);
                                    appendedValue = "";
                                }
                                else
                                {
                                    appendedValue += ",";
                                    appendedValue += value;
                                }
                            }

                            values = valueCorrection.ToArray();

                            ColorableObject obj;
                            if (values[fillAlphaIndex] != "")
                            {
                                //It has a color
                                obj = new ColorableObject(values[1], values[0], values[2], values[3], values[4],
                                    Color.FromArgb(Convert.ToInt32(values[fillAlphaIndex]),
                                    Convert.ToInt32(values[fillAlphaIndex + 1]),
                                    Convert.ToInt32(values[fillAlphaIndex + 2]),
                                    Convert.ToInt32(values[fillAlphaIndex + 3])));
                            }
                            else
                            {
                                //It doesn't have a color
                                obj = new ColorableObject(values[1], values[0], values[2], values[3], values[4]);
                            }

                            if (values[borderAlphaIndex] != "")
                            {
                                obj.BorderColor = Color.FromArgb(Convert.ToInt32(values[borderAlphaIndex]),
                                    Convert.ToInt32(values[borderAlphaIndex + 1]),
                                    Convert.ToInt32(values[borderAlphaIndex + 2]),
                                    Convert.ToInt32(values[borderAlphaIndex + 3]));
                            }

                            if (colorableObjects.ContainsKey(obj.Type))
                            {
                                //Category has been created already
                                colorableObjects[obj.Type][obj.Name] = obj;
                            }
                            else
                            {
                                //Category needs to be created
                                colorableObjects[obj.Type] = new Dictionary<string, ColorableObject>();
                                colorableObjects[obj.Type][obj.Name] = obj;
                            }
                            line = sr.ReadLine();
                        }
                        sr.Close();
                    }
                }

                return colorableObjects;
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return null;
            }
        }

        private Dictionary<string, Dictionary<string, ColorableObject>> SortObjectsProperly(Dictionary<string, Dictionary<string, ColorableObject>> colorableObjects)
        {
            string fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                //12 coming before 2 is bugging me when trying to color objects.
                Dictionary<string, Dictionary<string, ColorableObject>> fixedSort = new Dictionary<string, Dictionary<string, ColorableObject>>();

                foreach (var colorableCategory in colorableObjects)
                {
                    Dictionary<string, SortingObject> numericSortCorrecter = new Dictionary<string, SortingObject>();
                    foreach (string name in colorableCategory.Value.Keys)
                    {
                        int endNumber = 0;
                        int lastSuccessfulNumber = 0;
                        string endNumberString = "";
                        string beginningText = name;
                        while (beginningText.Length > 0 && int.TryParse(endNumberString = beginningText.Last() + endNumberString, out endNumber) && beginningText.Last() != ' ')
                        {
                            beginningText = name.Substring(0, beginningText.Length - 1);
                            lastSuccessfulNumber = endNumber;
                        }
                        SortingObject obj = new SortingObject(beginningText, lastSuccessfulNumber);
                        obj.TypeGrouping = colorableCategory.Value[name].TypeGrouping;
                        numericSortCorrecter[name] = obj;
                    }

                    IOrderedEnumerable<KeyValuePair<string, SortingObject>> sortedCollection = numericSortCorrecter
                        .OrderBy(x => x.Value.EndingNumber)
                        .OrderBy(x => x.Value.BeginningText)
                        .OrderBy(x => x.Value.TypeGrouping);

                    Dictionary<string, ColorableObject> resortedDict = new Dictionary<string, ColorableObject>();
                    foreach (var resortedString in sortedCollection)
                    {
                        if (colorableCategory.Value.ContainsKey(resortedString.Key))
                        {
                            resortedDict[resortedString.Key] = colorableCategory.Value[resortedString.Key];
                        }
                        else
                        {
                            //This really should never be hit but I'm leaving a breadcrumb for now just in case.
                            Console.WriteLine("Get mad @Sean! ColorManager.SortObjectsProperly missing values...");
                        }
                    }
                    fixedSort[colorableCategory.Key] = resortedDict;
                }
                return fixedSort;
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return colorableObjects;
            }
        }

        /// <summary>
        /// The ManyToOneLookupDict is used to color non-colorable objects with a seperate colorable object
        /// The use case is basically when an object gets created and destroyed frequently
        /// I use it for results-models. Results(many) are colored with their model(one)
        /// </summary>
        /// <param name="manyToOneLookupDict"></param>
        public void SaveManyToOneLookupDict(Dictionary<string, string> manyToOneLookupDict)
        {
            try
            {
                Directory.CreateDirectory(AppSettings.ColorDir);

                using (StreamWriter sw = new StreamWriter(AppSettings.ColorDir + "ManyToOneLookup.csv"))
                {
                    sw.WriteLine("\"LookupValue\",\"LookupKey\"");

                    foreach (KeyValuePair<string, string> resultModel in manyToOneLookupDict)
                    {
                        sw.WriteLine(string.Concat("\"", resultModel.Key, "\",\"", resultModel.Value, "\""));
                    }
                    sw.Close();
                }
            }
            catch
            {

            }
        }

        public Dictionary<string, string> LoadManyToOneLookupDict()
        {
            try
            {
                Dictionary<string, string> manyToOneLookupDict = new Dictionary<string, string>();

                string manyToOneLookupFile = AppSettings.ColorDir + "ManyToOneLookup.csv";

                if (File.Exists(manyToOneLookupFile))
                {
                    using (StreamReader sr = new StreamReader(manyToOneLookupFile))
                    {
                        string line = sr.ReadLine(); //header

                        line = sr.ReadLine();
                        line = sr.ReadLine();
                        while (line != null)
                        {
                            string[] values = line.Split(',');
                            List<string> valueCorrection = new List<string>();

                            string appendedValue = "";
                            foreach (string value in values)
                            {
                                if (value.Length < 2)
                                {
                                    if (!string.IsNullOrEmpty(value) && value != "\"")
                                    {
                                        appendedValue += ",";
                                        appendedValue += value;
                                        continue;
                                    }
                                    else if (!string.IsNullOrEmpty(appendedValue) && value == "\"")
                                    {
                                        appendedValue += ",";
                                        valueCorrection.Add(appendedValue);
                                        appendedValue = "";
                                        continue;
                                    }
                                    else
                                    {
                                        //I believe this shouldn't get hit but if the value is null, don't worry about it...
                                        continue;
                                    }
                                }

                                if (value.First() == '"' && value.Last() == '"')
                                {
                                    valueCorrection.Add(value.Substring(1, value.Length - 2));
                                }
                                else if (value.First() == '"')
                                {
                                    appendedValue = value.Substring(1, value.Length - 1);
                                }
                                else if (value.Last() == '"')
                                {
                                    appendedValue += ",";
                                    appendedValue += value.Substring(0, value.Length - 1);
                                    valueCorrection.Add(appendedValue);
                                    appendedValue = "";
                                }
                                else
                                {
                                    appendedValue += ",";
                                    appendedValue += value;
                                }
                            }

                            values = valueCorrection.ToArray();

                            manyToOneLookupDict[values[0]] = values[1];
                            line = sr.ReadLine();
                        }

                        while (line != null)
                        {
                            string[] values = line.Split(',');

                            manyToOneLookupDict[values[0]] = values[1];

                            line = sr.ReadLine();
                        }
                        sr.Close();
                    }
                }

                return manyToOneLookupDict;
            }
            catch
            {
                return null;
            }
        }
    }
    class SortingObject
    {
        public string TypeGrouping { get; set; }
        public string BeginningText { get; set; }
        public int EndingNumber { get; set; }

        public SortingObject(string beginningText = "_", int endingNumber = -1)
        {
            //The defaults are there because if the object being sorted doesn't have either, it should show above objects that do.
            //For example, 123 should be above a123. abc should show above abc1.
            BeginningText = beginningText;
            EndingNumber = endingNumber;
        }
    }
}
