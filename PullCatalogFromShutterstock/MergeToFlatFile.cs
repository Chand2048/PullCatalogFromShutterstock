using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace PullCatalogFromShutterstock
{
    class MergeToFlatFile
    {
        private List<Photo> photos = new List<Photo>();

        public void Add(string filename)
        {
            Photo p = new Photo();
            p.ExtractFromFile(filename);
            this.photos.Add(p);
        }

        public void Save(string filename)
        {
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            using (TextWriter writer = File.CreateText(filename))
            {
                foreach(Photo p in this.photos)
                {
                    p.Write(writer);
                }

                writer.Close();
            }
        }

        private class Photo
        {
            public string id;
            public string status;
            public string category1;
            public string category2;
            public string description;
            public bool isEditorial;
            public string keywords;
            public string filename;
            public string uploadDate;
            public string thumbnailURL;
            public string thumbnailURL480;

            public void ExtractFromFile(string filename)
            {
                using (TextReader r = File.OpenText(filename))
                {
                    string data = r.ReadToEnd();
                    this.ExtractFromJson(data);
                }
            }

            public void Write(TextWriter writer)
            {
                char delim = '\t';
                writer.Write(this.id); writer.Write(delim);
                writer.Write(this.status); writer.Write(delim);
                writer.Write(this.category1); writer.Write(delim);
                writer.Write(this.category2); writer.Write(delim);
                writer.Write(this.description); writer.Write(delim);
                writer.Write(this.isEditorial); writer.Write(delim);
                writer.Write(this.keywords); writer.Write(delim);
                writer.Write(this.filename); writer.Write(delim);
                writer.Write(this.uploadDate); writer.Write(delim);
                writer.Write(this.thumbnailURL); writer.Write(delim);
                writer.Write(this.thumbnailURL480); writer.Write(delim);
                writer.Write("\r\n");

                writer.Flush();
            }

            private void parseID(JObject root)
            {
                this.id = this.getText(root, "data.id");
                if (this.id.Length > 0 && this.id[0] == 'P')
                {
                    this.id = this.id.Substring(1);
                }
            }

            private void parseKeywords(JObject root)
            {
                JToken j = root.SelectToken("data.keywords");
                StringBuilder builder = new StringBuilder();
                foreach (JToken kw in j.Children())
                {
                    if (builder.Length > 0)
                    {
                        builder.Append("~");
                    }

                    builder.Append(kw.ToString());
                }

                this.keywords = builder.ToString();
            }

            private bool ExtractFromJson(string data)
            {
                JObject root = JsonConvert.DeserializeObject(data) as JObject;

                this.parseID(root);
                this.parseKeywords(root);

                this.status = this.getText(root, "data.status");
                this.category1 = this.getText(root, "data.categories[0]");
                this.category2 = this.getText(root, "data.categories[1]");
                this.description = this.getText(root, "data.description");
                this.isEditorial = this.getBool(root, "data.is_editorial");
                this.filename = this.getText(root, "data.original_filename");
                this.uploadDate = this.getText(root, "data.uploaded_date");
                this.thumbnailURL = "https:" + this.getText(root, "data.thumbnail_url");
                this.thumbnailURL480 = this.getText(root, "data.thumbnail_url_480");

                return true;
            }

            private string getText(JToken root, string path)
            {
                JToken x = root.SelectToken(path);
                if (x != null)
                {
                    return x.ToString();
                }

                return string.Empty;
            }

            private bool getBool(JToken root, string path)
            {
                string x = this.getText(root, path);
                bool t;
                if (bool.TryParse(x, out t))
                {
                    return t;
                }

                return false;
            }
        }
    }
}
