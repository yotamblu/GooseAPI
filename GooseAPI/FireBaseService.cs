using FireSharp.Interfaces;
using FireSharp.Response;
using FireSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FireSharp.Config;
using FireSharp.Response;
using FireSharp.Interfaces;
using System.Drawing.Printing;
using FireSharp;
using Newtonsoft.Json;
using System.IO;
using System.Runtime.CompilerServices;
using FireSharp.Extensions;

namespace GooseAPI
{
    public class FirebaseService
    {
        private IFirebaseClient _client;

        public FirebaseService()
        {





            _client = new FirebaseClient(FireBaseConfig.config);

            if (_client == null)
            {
                throw new Exception("Unable to connect to Firebase. Check your configuration.");
            }

        }

        public void InsertData(string path, object data)
        {
            try
            {
                // Insert data into Firebase
                SetResponse response = _client.Set(path, data);

                // Log a success message
                Console.WriteLine($"Data inserted successfully at path: {path}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inserting data: {ex.Message}");
            }
        }

        public T GetData<T>(string path)
        {
            FirebaseResponse response = _client.Get(path);
            try
            {
                return response.ResultAs<T>();

            }
            catch (NullReferenceException ex)
            {
                return default(T);
            }


        }

        public void DeleteData(string path)
        {

            _client.Delete(path);
        }

    }
};
