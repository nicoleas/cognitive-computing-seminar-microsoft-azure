# cognitive-computing-seminar-microsoft-azure
Created using: 
https://docs.microsoft.com/en-us/azure/cognitive-services/Computer-vision/quickstarts/csharp-analyze
https://docs.microsoft.com/en-us/azure/cognitive-services/Computer-vision/quickstarts/csharp-hand-text


Prerequisites:
You must have Visual Studio 2015 or later
You must have a subscription key for computer vision. To get a subscription key, see https://docs.microsoft.com/en-us/azure/cognitive-services/Computer-vision/vision-api-how-to-topics/howtosubscribe.

To create this application in Visual Studio, follow the following steps: 
1.	Create a new Visual Studio solution in Visual Studio, using the Visual C# Console App (.NET Framework) template.
2.	Install NuGet packages:
o	 Newtonsoft.Json  
o	Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction 
o	Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training 
 
3.	As global variables to the Program class, add the following code and replace the subscriptionKey with your subscription key. Replace the value of uriBase with the endpoint URL for your region obtained from https://westus.dev.cognitive.microsoft.com/docs/services/5adf991815e1060e6355ad44/operations/2afb498089f74080d7ef85eb.  
 https://i.imgur.com/sAtNBKe.png
 
4.	In the main method, define the following variables and replace the trainingKey and predictionKey with your own. 
 https://i.imgur.com/9YOuA2l.png
 
5.	Now it is time to create the project and add tags. To do this, append this code to the main method:
 https://i.imgur.com/7bCSwWf.png
 
6.	Add the images you would like to train. In this case, create an Images folder with subfolders for each class of fire extinguisher and the fire extinguisher itself. The folder structure is shown:
https://i.imgur.com/K35vWRs.png
 
7.	We will now tell the user that we are uploading the images, and then upload them from the disk. Add the following function call and function: 
 https://i.imgur.com/Xw4BgHN.png
 https://i.imgur.com/zE5iqnD.png
 
8.	Each folder of images can be uploaded in a batch and assigned a tag. Do the following for each tag you’ve declared.
https://i.imgur.com/pv8vMoL.png
 
9.	Once the images are loaded and tagged, it’s time for Azure to start training. Append the following code again in the main method:
 https://i.imgur.com/WAuC0lK.png
 
10.	Each time Azure trains a model, it creates an iteration to be published and referenced later for training. Publish your iteration, and communicate to the user that the Loading, Training and Publishing of the service is finished. Replace the predictionResourceId with your id.
https://i.imgur.com/IN4cbix.png
 
11.	Now let’s set up our application for prediction and get an image to be predicted from the user.
https://i.imgur.com/URPtqVq.png
 
12.	Complete the code of the main method by adding references to the predictions methods if the file the user has indicated is valid and exists.
https://i.imgur.com/GcbS3wS.png
 
13.	The first method, ClassifyImage, is Microsoft Azure’s call to the custom vision endpoint. It will return a result classifying the image submitted. The results are returned and saved to the classificationResults variable for future use.
https://i.imgur.com/dB7OOrx.png
 
14.	Next, the ReadHandwrittenText method will take us to the OCR handwritten text recognition service. 
In this method, two REST API calls are made. One will submit the image for processing and the other will receive the text found in the image. Following the retrieval of data, the application then extracts the single “text” strings from the JToken object and adds them to our previously defined array. This text will be used later on to determine the class of the fire extinguisher by finding references of “A” “B” “C” “D” or “K”.
 https://i.imgur.com/ol3pKlG.png
 https://i.imgur.com/FYoKSj9.png
 https://i.imgur.com/NAvNxgU.png
 https://i.imgur.com/3xdeTdj.png
 https://i.imgur.com/8rabiGh.png
 
15.	The GetImageAsByteArray method is shown here:
https://i.imgur.com/at38hGa.png
 
16.	Now that we have both of our prediction results with the Custom Image Classification model and the OCR, we can use that data to logically classify our extinguishers. Add the DetermineExtinguisherType method. This method will loop through the results produced from the custom vision model, and only predict the class of the fire extinguisher if it has been determined the image submitted by the user for prediction is a fire extinguisher with a confidence of 70% or more. 
 https://i.imgur.com/0cKJGQw.png
 
17.	DetermineExtinguisherType takes us to PredictExtinguisherClasses, which takes in the extracted text from the OCR result in a list and for each text element assigns a level of confidence based on the length of the string. This is not ideal, but it is my work-around for the inconsistent results returned from the OCR. The idea is for example, the string “A” will be marked with a 100% confidence of Class A. The string “//A)/” will get marked with a 50% confidence of Class A, same as “CLASS” would get marked with a 50% confidence of Class A. 
https://i.imgur.com/8WinT0q.png
 
18.	The CreatePrediction method will contain if statements to find “A” “B” “C” “D” or “E” within the given string. It will then determine if the prediction made is a duplicate of the class. If it is a duplicate, the confidence will get replaced with the highest confidence. If it is not a duplicate the prediction will simply be added to the dictionary of predictions.  
 https://i.imgur.com/ErPaqR4.png
 
19.	The CheckForExistingKey is shown below in it’s entirety:
 https://i.imgur.com/yUaWGTr.png
 https://i.imgur.com/3BEYdWK.png
 
20.	Lastly, the results are displayed.
https://i.imgur.com/Q8Jer64.png
 
21.	Run the application and upload a photo from a local path on your computer.

