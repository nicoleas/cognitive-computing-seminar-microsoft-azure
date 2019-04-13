using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using System.Collections.Generic;
using System.Threading;

namespace CSHttpClientSample
{
    class Program
    {
        const string subscriptionKey = "20ec8e2097ba4fc3b490c6edfaaac613";
        const string uriBase =
            "https://westcentralus.api.cognitive.microsoft.com/vision/v2.0/read/core/asyncBatchAnalyze";
        private const string SouthCentralUsEndpoint = "https://southcentralus.api.cognitive.microsoft.com";

        private static MemoryStream testImage;
        private static List<string> fireExtinguisherImages;
        private static List<string> classALogoImages;
        private static List<string> classBLogoImages;
        private static List<string> classCLogoImages;
        private static List<string> classDLogoImages;
        private static List<string> classKLogoImages;

        private static Dictionary<int, Dictionary<string, double>> prediction;

        static void Main()
        {
            string trainingKey = "4913ffd2f53c4ec2baf2fc5422d5e8fe";
            string predictionKey = "e53ac9b197d64b90b26ba90accf4d973";
            Dictionary<string, double> classificationResults = new Dictionary<string, double>();
            List<string> textResults = new List<string>();

            // Create the Api, passing in the training key
            CustomVisionTrainingClient trainingApi = new CustomVisionTrainingClient()
            {
                ApiKey = trainingKey,
                Endpoint = SouthCentralUsEndpoint
            };

            // Create a new project
            Console.WriteLine("Creating new project:");
            var project = trainingApi.CreateProject("Fire Extinguisher");

            // Make tags in the new project
            var fireExtinguisherTag = trainingApi.CreateTag(project.Id, "Fire Extinguisher");
            var classALogoTag = trainingApi.CreateTag(project.Id, "Class A Logo");
            var classBLogoTag = trainingApi.CreateTag(project.Id, "Class B Logo");
            var classCLogoTag = trainingApi.CreateTag(project.Id, "Class C Logo");
            var classDLogoTag = trainingApi.CreateTag(project.Id, "Class D Logo");
            var classKLogoTag = trainingApi.CreateTag(project.Id, "Class K Logo");

            // Add some images to the tags
            Console.WriteLine("\tUploading images");
            LoadImagesFromDisk();

            //image uploaded in a batch
            var imageFiles1 = fireExtinguisherImages.Select(img => new ImageFileCreateEntry(Path.GetFileName(img), File.ReadAllBytes(img))).ToList();
            trainingApi.CreateImagesFromFiles(project.Id, new ImageFileCreateBatch(imageFiles1, new List<Guid>() { fireExtinguisherTag.Id }));

            var imageFiles2 = classALogoImages.Select(img => new ImageFileCreateEntry(Path.GetFileName(img), File.ReadAllBytes(img))).ToList();
            trainingApi.CreateImagesFromFiles(project.Id, new ImageFileCreateBatch(imageFiles2, new List<Guid>() { classALogoTag.Id }));

            var imageFiles3 = classBLogoImages.Select(img => new ImageFileCreateEntry(Path.GetFileName(img), File.ReadAllBytes(img))).ToList();
            trainingApi.CreateImagesFromFiles(project.Id, new ImageFileCreateBatch(imageFiles3, new List<Guid>() { classBLogoTag.Id }));

            var imageFiles4 = classCLogoImages.Select(img => new ImageFileCreateEntry(Path.GetFileName(img), File.ReadAllBytes(img))).ToList();
            trainingApi.CreateImagesFromFiles(project.Id, new ImageFileCreateBatch(imageFiles4, new List<Guid>() { classCLogoTag.Id }));

            var imageFiles5 = classDLogoImages.Select(img => new ImageFileCreateEntry(Path.GetFileName(img), File.ReadAllBytes(img))).ToList();
            trainingApi.CreateImagesFromFiles(project.Id, new ImageFileCreateBatch(imageFiles5, new List<Guid>() { classDLogoTag.Id }));

            var imageFiles6 = classKLogoImages.Select(img => new ImageFileCreateEntry(Path.GetFileName(img), File.ReadAllBytes(img))).ToList();
            trainingApi.CreateImagesFromFiles(project.Id, new ImageFileCreateBatch(imageFiles6, new List<Guid>() { classKLogoTag.Id }));

            // Now there are images with tags start training the project
            Console.WriteLine("\tTraining");
            var iteration = trainingApi.TrainProject(project.Id);

            // The returned iteration will be in progress, and can be queried periodically to see when it has completed
            while (iteration.Status == "Training")
            {
                Thread.Sleep(1000);

                // Re-query the iteration to get it's updated status
                iteration = trainingApi.GetIteration(project.Id, iteration.Id);
            }

            // Identify the iteration and publish it to the prediction end point
            var publishedModelName = "fireExtinguisherClassModel";
            var predictionResourceId = "/subscriptions/c6cd8af1-b389-4372-8787-8629ead7098b/resourceGroups/FireEvacuationResource/providers/Microsoft.CognitiveServices/accounts/FireEvacuationResource_prediction";
            trainingApi.PublishIteration(project.Id, iteration.Id, publishedModelName, predictionResourceId);
            Console.WriteLine("Done!\n");

            // Create a prediction endpoint, passing in obtained prediction key
            CustomVisionPredictionClient endpoint = new CustomVisionPredictionClient()
            {
                ApiKey = predictionKey,
                Endpoint = SouthCentralUsEndpoint
            };

            // Get the path and filename to process from the user.
            Console.Write(
                "Enter the path to an image you would like to evaluate: ");
            string imageFilePath = Console.ReadLine();

            if (File.Exists(imageFilePath))
            {
                Console.WriteLine("\nWait a moment for the results to appear.\n");

                // Get image
                testImage = new MemoryStream(File.ReadAllBytes(Path.Combine("Images", imageFilePath)));

                classificationResults = ClassifyImage(endpoint, project, publishedModelName, classificationResults).Result;
                textResults = ReadHandwrittenText(imageFilePath, textResults).Result;

                prediction = new Dictionary<int, Dictionary<string, double>>();

                DetermineExtinguisherType(classificationResults, textResults);
            }
            else
            {
                Console.WriteLine("\nInvalid file path");
            }
            Console.WriteLine("\nPress Enter to exit...");
            Console.ReadLine();
        }

        private static void LoadImagesFromDisk()
        {
            // this loads the images to be uploaded from disk into memory
            fireExtinguisherImages = Directory.GetFiles(Path.Combine("Images", "Fire Extinguisher")).ToList();
            classALogoImages = Directory.GetFiles(Path.Combine("Images", "Class A Logo")).ToList();
            classBLogoImages = Directory.GetFiles(Path.Combine("Images", "Class B Logo")).ToList();
            classCLogoImages = Directory.GetFiles(Path.Combine("Images", "Class C Logo")).ToList();
            classDLogoImages = Directory.GetFiles(Path.Combine("Images", "Class D Logo")).ToList();
            classKLogoImages = Directory.GetFiles(Path.Combine("Images", "Class K Logo")).ToList();
        }

        static async Task<Dictionary<string, double>> ClassifyImage(CustomVisionPredictionClient endpoint, 
            Project project, string publishedModelName, Dictionary<string, double> classificationResults)
        {
            // Make a prediction with given image
            var result = endpoint.ClassifyImage(project.Id, publishedModelName, testImage);

            // Loop over each prediction and write out the results
            foreach (var c in result.Predictions)
            {
                classificationResults.Add(c.TagName, c.Probability);
            }

            return classificationResults;
        }

        /// <summary>
        /// Gets the handwritten text from the specified image file by using
        /// the Computer Vision REST API.
        /// </summary>
        /// <param name="imageFilePath">The image file with handwritten text.</param>
        static async Task<List<string>> ReadHandwrittenText(string imageFilePath, List<string>textResults)
        {
            try
            {
                HttpClient client = new HttpClient();

                // Request headers.
                client.DefaultRequestHeaders.Add(
                    "Ocp-Apim-Subscription-Key", subscriptionKey);

                // Request parameter.
                string requestParameters = "mode=Handwritten";

                // Assemble the URI for the REST API method.
                string uri = uriBase + "?" + requestParameters;

                HttpResponseMessage response;

                // Two REST API methods are required to extract handwritten text.
                // One method to submit the image for processing, the other method
                // to retrieve the text found in the image.

                // operationLocation stores the URI of the second REST API method,
                // returned by the first REST API method.
                string operationLocation;

                // Reads the contents of the specified local image
                // into a byte array.
                byte[] byteData = GetImageAsByteArray(imageFilePath);

                // Adds the byte array as an octet stream to the request body.
                using (ByteArrayContent content = new ByteArrayContent(byteData))
                {
                    // This example uses the "application/octet-stream" content type.
                    // The other content types you can use are "application/json"
                    // and "multipart/form-data".
                    content.Headers.ContentType =
                        new MediaTypeHeaderValue("application/octet-stream");

                    // The first REST API method, Batch Read, starts
                    // the async process to analyze the written text in the image.
                    response = await client.PostAsync(uri, content);
                }

                // The response header for the Batch Read method contains the URI
                // of the second method, Read Operation Result, which
                // returns the results of the process in the response body.
                // The Batch Read operation does not return anything in the response body.
                if (response.IsSuccessStatusCode)
                    operationLocation =
                        response.Headers.GetValues("Operation-Location").FirstOrDefault();
                else
                {
                    // Display the JSON error data.
                    string errorString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("\n\nResponse:\n{0}\n",
                        JToken.Parse(errorString).ToString());
                    return null;
                }

                // If the first REST API method completes successfully, the second 
                // REST API method retrieves the text written in the image.
                //
                // Note: The response may not be immediately available. Handwriting
                // recognition is an asynchronous operation that can take a variable
                // amount of time depending on the length of the handwritten text.
                // You may need to wait or retry this operation.
                //
                // This example checks once per second for ten seconds.
                string contentString;
                int i = 0;
                do
                {
                    System.Threading.Thread.Sleep(1000);
                    response = await client.GetAsync(operationLocation);
                    contentString = await response.Content.ReadAsStringAsync();
                    ++i;
                }
                while (i < 10 && contentString.IndexOf("\"status\":\"Succeeded\"") == -1);

                if (i == 10 && contentString.IndexOf("\"status\":\"Succeeded\"") == -1)
                {
                    Console.WriteLine("\nTimeout error.\n");
                    return null;
                }


                // Put extracted text results into List
                foreach(JToken obj in JToken.Parse(contentString)["recognitionResults"])
                {
                    foreach(JToken obj2 in obj["lines"])
                    {
                        textResults.Add(obj2["text"].ToString());

                        foreach(JToken obj3 in obj2["words"])
                        {
                            textResults.Add(obj3["text"].ToString());
                        }
                    }
                    
                }

                return textResults;

            }
            catch (Exception e)
            {
                Console.WriteLine("\n" + e.Message);
                return null;
            }

            
        }

        /// <summary>
        /// Returns the contents of the specified file as a byte array.
        /// </summary>
        /// <param name="imageFilePath">The image file to read.</param>
        /// <returns>The byte array of the image data.</returns>
        static byte[] GetImageAsByteArray(string imageFilePath)
        {
            // Open a read-only file stream for the specified file.
            using (FileStream fileStream =
                new FileStream(imageFilePath, FileMode.Open, FileAccess.Read))
            {
                // Read the file's contents into a byte array.
                BinaryReader binaryReader = new BinaryReader(fileStream);
                return binaryReader.ReadBytes((int)fileStream.Length);
            }
        }

        static void PrintResults(Dictionary<string, double> classificationResults, List<string> textResults, Dictionary<string, double> finalPrediction)
        {
            int i, j;

            Console.WriteLine("\nFrom Microsoft Azure Custom Vision image classificaiton results: ");
            foreach (KeyValuePair<string, double> classification in classificationResults) {
                Console.WriteLine("Classification: {0}\nConfidence: {1}", classification.Key, classification.Value);
            }

            Console.WriteLine("\n...And Microsoft Azure Optical Character Recognition (Handwriting) results: ");
            for (j = 0; j < textResults.Count; j++)
            {
                Console.WriteLine("    {0}", textResults[j]);
            }

            Console.WriteLine("\nThe Fire Evacuation Cognitive Vision application has determined that: ");
            if(finalPrediction != null)
            {
                foreach (KeyValuePair<string, double> pred in finalPrediction)
                {
                    Console.WriteLine("The image is a class {0} fire extinguisher with a confidence of: {1}", pred.Key, pred.Value);
                }
            } else
            {
                Console.WriteLine("The image provided is likely a fire extinguisher however we could not identify the class.");
            }
        }

        static void DetermineExtinguisherType (Dictionary<string, double> classificationResults, List<string> textResults) {
            Dictionary<string, double> finalPrediction = new Dictionary<string, double>();

            foreach (KeyValuePair<string, double> classification in classificationResults)
            {
                // If classification is confidently a fire extinguisher
                if(classification.Key == "Fire Extinguisher" && classification.Value >= 0.7)
                {
                    PredictExtinguisherClasses(textResults);
                    finalPrediction = FilterPredictions(finalPrediction).Result;

                    PrintResults(classificationResults, textResults, finalPrediction);
                } else if (classification.Key == "Fire Extinguisher" && classification.Value < 0.7)
                {
                    Console.WriteLine("\nThe fire extinguisher classification could not be completed. Either the " +
                        "image provided is not a fire extinguisher or the photo is not clear enough.");
                }
            }
        }

        static void PredictExtinguisherClasses(List<string> textResults)
        {
            double confidence = 0;
            int counter = 0;

            foreach(string text in textResults)
            {
                switch (text.Length)
                {
                    case 1:
                        confidence = 1.00;
                        CreatePrediction(text, confidence, counter);
                        break;
                    case 2:
                        confidence = 0.90;
                        CreatePrediction(text, confidence, counter);
                        break;
                    case 3:
                        confidence = 0.80;
                        CreatePrediction(text, confidence, counter);
                        break;
                    case 4:
                        confidence = 0.60;
                        CreatePrediction(text, confidence, counter);
                        break;
                    case 5:
                        confidence = 0.50;
                        CreatePrediction(text, confidence, counter);
                        break;
                    default:
                        confidence = 0.10;
                        CreatePrediction(text, confidence, counter);
                        break;
                }
                counter++;
            }
        }

        static void CreatePrediction(string text, double confidence, int counter)
        {
            // Look for text that could resemble the fire extinguisher class and 
            // determine if it is a duplicate before adding to the dictionary
            if (text.Contains("A") && !(text.Contains("B") || text.Contains("C") || text.Contains("D") || text.Contains("K")))
            {
                Dictionary<string, double> newPrediction = new Dictionary<string, double>();
                newPrediction = CheckForExistingKey("A", confidence, text).Result;

                if (newPrediction != null)
                {
                    prediction.Add(counter, newPrediction);
                }
            }
            else if (text.Contains("B") && !(text.Contains("A") || text.Contains("C") || text.Contains("D") || text.Contains("K")))
            {
                Dictionary<string, double> newPrediction = new Dictionary<string, double>();
                newPrediction = CheckForExistingKey("B", confidence, text).Result;

                if (newPrediction != null)
                {
                    prediction.Add(counter, newPrediction);
                }
            }
            else if (text.Contains("C") && !(text.Contains("A") || text.Contains("B") || text.Contains("D") || text.Contains("K")))
            {
                Dictionary<string, double> newPrediction = new Dictionary<string, double>();
                newPrediction = CheckForExistingKey("C", confidence, text).Result;

                if (newPrediction != null)
                {
                    prediction.Add(counter, newPrediction);
                }
            }
            else if (text.Contains("D") && !(text.Contains("A") || text.Contains("B") || text.Contains("C") || text.Contains("K")))
            {
                Dictionary<string, double> newPrediction = new Dictionary<string, double>();
                newPrediction = CheckForExistingKey("D", confidence, text).Result;

                if (newPrediction != null)
                {
                    prediction.Add(counter, newPrediction);
                }
            }
            else if (text.Contains("K") && !(text.Contains("A") || text.Contains("B") || text.Contains("C") || text.Contains("D")))
            {
                Dictionary<string, double> newPrediction = new Dictionary<string, double>();
                newPrediction = CheckForExistingKey("K", confidence, text).Result;

                if (newPrediction != null)
                {
                    prediction.Add(counter, newPrediction);
                }
            } else
            {
                Dictionary<string, double> newPrediction = new Dictionary<string, double>();
                newPrediction = CheckForExistingKey("", confidence, text).Result;

                if (newPrediction != null)
                {
                    prediction.Add(counter, newPrediction);
                }
            }
        }

        static async Task<Dictionary<string, double>> CheckForExistingKey(string key, double confidence, string text)
        {
            Dictionary<string, double> highestPrediction = new Dictionary<string, double>();
            Boolean sameKeys = false;

            if(prediction != null && prediction.Count != 0)
            {
                foreach (KeyValuePair<int, Dictionary<string, double>> pred in prediction)
                {
                    foreach (KeyValuePair<string, double> p in pred.Value)
                    {
                        if (p.Key == key)
                        {
                            sameKeys = true;
                            if (key != "")
                            {
                                if (p.Value > confidence)
                                {
                                    highestPrediction = null;
                                }
                                else if (p.Value < confidence)
                                {
                                    highestPrediction.Add(key, confidence);
                                    pred.Value.Remove(p.Key);
                                    prediction.Remove(pred.Key);
                                }

                                return highestPrediction;
                            } else
                            {
                                return null;
                            }
                        }
                    }
                }
            } else
            {
                highestPrediction.Add(key, confidence);
                return highestPrediction;
            }

            if (!sameKeys)
            {
                highestPrediction.Add(key, confidence);
                return highestPrediction;
            }
            else {
                return null;
            }
        }

        static async Task<Dictionary<string, double>> FilterPredictions(Dictionary<string, double> finalPrediction)
        {

            foreach(KeyValuePair<int, Dictionary<string, double>> newPred in prediction)
            {
                foreach(KeyValuePair<string, double> p in newPred.Value)
                {
                    if (p.Key != "")
                    {
                        finalPrediction.Add(p.Key, p.Value);
                    }
                }
            }

            if (finalPrediction.Count > 0)
            {
                return finalPrediction;
            }
            else
            {
                return null;
            }
        }

    }
}