using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Configuration;

namespace MyBot.Dialogs
{   
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var inboundMessage = await result as Activity;
            var outboundMessage = context.MakeMessage();

            // calculate something for us to return
            int length = (inboundMessage.Text ?? string.Empty).Length;

            if (inboundMessage.Text.ToUpper().Contains("HI"))
                await context.PostAsync($"Hello there! How can I assist you today?");
            else if (inboundMessage.ChannelId == "sms")
            {
                //if twilio does not support mms, then send text
                outboundMessage.Text = "Noor needs to upgrade his Twilio to support MMS :-(";
            }
            //else if(true)
            //{
            //    var output = NLPProcessor.GetResponse(inboundMessage.Text);
            //    await context.PostAsync(output);
            //}
            else
            {
                try
                {
                    //get a random gif from giphy.com and send it as a card
                    //Trending GIF API:http://api.giphy.com/v1/gifs/trending?api_key=
                    //Search GIF API: http://api.giphy.com/v1/gifs/search?q=funny+cat&api_key= 

                    var client = new HttpClient() { BaseAddress = new Uri("http://api.giphy.com") };
                    
                    var resultJson = client.GetStringAsync($"/v1/gifs/search?q={inboundMessage.Text.Replace(" ","+")}&api_key={ConfigurationManager.AppSettings["GiphyApiKey"]}").Result;
                    var data = ((dynamic)JObject.Parse(resultJson)).data;
                    var gif = data[(int)Math.Floor(new Random().NextDouble() * data.Count)];
                    var gifUrl = gif.images.fixed_height.url.Value;
                    var slug = gif.slug.Value;

                    outboundMessage.Attachments = new List<Attachment>() { new Attachment()
                {
                    ContentUrl = gifUrl,
                    ContentType = "image/gif",
                    Name = slug + ".gif"
                }
                };
                }

                catch(Exception ex)
                {
                    outboundMessage.Text = ($"You sent {inboundMessage.Text} which was {length} characters.error:{ex.Message}");
                }
                
            }
            // return our reply to the user
            await context.PostAsync(outboundMessage);
            context.Wait(MessageReceivedAsync);
        }
    }
}