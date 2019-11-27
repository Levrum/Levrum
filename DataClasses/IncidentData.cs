using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Levrum.Data.Classes
{
    public class IncidentData
    {
        private char[] m_id;
        private long m_time;
        private char[] m_location;

        public string Id
        {
            get
            {
                return new string(m_id);
            }
            set
            {
                m_id = value.ToCharArray();
            }
        }

        public DateTime Time
        {
            get
            {
                return new DateTime(m_time);
            }
            set
            {
                m_time = value.Ticks;
            }
        }

        public string Location
        {
            get
            {
                return new string(m_location);
            }
            set
            {
                m_location = value.ToCharArray();
            }
        }

        public double Longitude { get; set; } = 0.0;
        public double Latitude { get; set; } = 0.0;

        private InternedDictionary<string, object> m_data = null;

        public InternedDictionary<string, object> Data
        {
            get
            {
                if (m_data == null)
                    m_data = new InternedDictionary<string, object>();

                return m_data;
            }

            protected set
            {
                m_data = value;
            }
        }

        public void Intern()
        {
            if (m_data != null && m_data.Count > 0)
            {
                m_data.Intern();
            }
            else
            {
                m_data = null;
            }

            foreach (ResponseData response in Responses)
            {
                response.Intern();
            }
        }


        public DataSet<ResponseData> Responses { get; set; }

        public IncidentData()
        {
            Responses = new DataSet<ResponseData>(this);
        }

        // 20190629 CDN - modified parameter defaults to compile in VS2017.   DateTime
        // default is a hack suggested by https://stackoverflow.com/questions/3031110/set-default-value-for-datetime-in-optional-parameter
        public IncidentData(string id = "", DateTime? time = null, string location = "", double longitude = 0.0, double latitude = 0.0, 
            IDictionary<string, object> data = null, IEnumerable<ResponseData> responses = null)
        {
            Id = id;
            Location = location;
            Longitude = longitude;
            Latitude = latitude;

            if (time > DateTime.MinValue)
                Time = (DateTime)time;

            if (data != null)
            {
                foreach (KeyValuePair<string, object> kvp in data)
                {
                    Data.Add(kvp.Key, kvp.Value);
                }
            }

            Responses = new DataSet<ResponseData>(this);

            if (responses != null)
                Responses.AddRange(responses);

            Intern();
        }

        public IncidentData(IncidentData source)
        {
            Id = source.Id;
            Time = source.Time;
            Location = source.Location;
            Longitude = source.Longitude;
            Latitude = source.Latitude;

            foreach (KeyValuePair<string, object> kvp in source.Data)
            {
                Data.Add(kvp.Key, kvp.Value);
            }

            Responses = new DataSet<ResponseData>(this);

            Responses.AddRange(source.Responses);

            Intern();
        }

        public bool AddDataElements(object oSource, params string[] oFieldNames)
        {
            Type srctype = oSource.GetType();
            foreach (string sfield in oFieldNames)
            {
                FieldInfo fi =  srctype.GetField(sfield);
                if (null == fi) { continue; }
                object oval = fi.GetValue(oSource);
                if (null == oval) { continue; }
                Data.Add(sfield,oval);   
            }
            return(true);
        }

    }
}
