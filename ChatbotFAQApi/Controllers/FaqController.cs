using ChatbotFAQApi.Models;
using ChatbotFAQApi.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ChatbotFAQApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FaqController : ControllerBase
    {
        private readonly FaqService _faqService;
        private readonly ChatSessionService _chatSessionService;

        public FaqController(FaqService faqService, ChatSessionService chatSessionService)
        {
            _faqService = faqService;
            _chatSessionService = chatSessionService;
        }

        // Helper method to assign unique IDs to options recursively/Recursive ID assignment method
        private void AssignOptionIdsRecursively(List<FaqOption>? options)
        {
            if (options == null) return;

            foreach (var option in options)
            {
                if (string.IsNullOrEmpty(option.Id))
                {
                    option.Id = ObjectId.GenerateNewId().ToString();
                }

                // Recurse for sub-options/Recurse into nested options
                AssignOptionIdsRecursively(option.Options);
            }
        }
        // Method to find a match in FAQs recursively
        private (string reply, List<string> options)? FindMatchRecursive(List<FaqItem> faqs, string message)
        {
            foreach (var faq in faqs)
            {
                // Match the question itself
                if (faq.Query.Equals(message, StringComparison.OrdinalIgnoreCase))
                {
                    var opts = faq.Options?.Select(o => o.OptionText).ToList() ?? new List<string>();
                    return (faq.Response, opts);
                }

                // Search inside options recursively
                if (faq.Options != null)
                {
                    foreach (var option in faq.Options)
                    {
                        var result = FindInOption(option, message);
                        if (result != null)
                            return result;
                    }
                }
            }
            return null;
        }

        private (string reply, List<string> options)? FindInOption(FaqOption option, string message)
        {
            if (option.OptionText.Equals(message, StringComparison.OrdinalIgnoreCase))
            {
                var opts = option.Options?.Select(o => o.OptionText).ToList() ?? new List<string>();
                return (option.Response, opts);
            }

            if (option.Options != null)
            {
                foreach (var nested in option.Options)
                {
                    var result = FindInOption(nested, message);
                    if (result != null)
                        return result;
                }
            }
            return null;
        }


        //GET All FAQs
        [HttpGet]
        public async Task<AiResponse<List<FaqItem>>> Get()
        {
            var res = new AiResponse<List<FaqItem>>();
            try
            {
                var faqs = await _faqService.GetAsync();
                res.Message = "Data fetched successfully";
                res.Status = true;
                res.Result = faqs;
            }
            catch (Exception ex)
            {
                res.Message = "Error: " + ex.Message;
                res.Status = false;
            }
            return res;
        }
        //GET Single FAQ by ID
        [HttpGet("{id}")]
        public async Task<AiResponse<FaqItem>> GetById(string id)
        {
            var res = new AiResponse<FaqItem>();
            try
            {
                var faq = await _faqService.GetByIdAsync(id);
                if (faq == null)
                {
                    res.Message = "FAQ not found";
                    res.Status = false;
                }
                else
                {
                    res.Message = "Data fetched successfully";
                    res.Status = true;
                    res.Result = faq;
                }
            }
            catch (Exception ex)
            {
                res.Message = "Error: " + ex.Message;
                res.Status = false;
            }
            return res;
        }
        //POST Single FAQ
        [HttpPost]
        public async Task<AiResponse<FaqItem>> Post([FromBody] FaqItem faq)
        {
            var res = new AiResponse<FaqItem>();
            try
            {
                AssignOptionIdsRecursively(faq.Options);
                await _faqService.CreateAsync(faq);
                res.Message = "FAQ created successfully";
                res.Status = true;
                res.Result = faq;
            }
            catch (Exception ex)
            {
                res.Message = "Error: " + ex.Message;
                res.Status = false;
            }
            return res;
        }
        //POST Bulk FAQs
        [HttpPost("bulk")]
        public async Task<AiResponse<string>> PostBulkFaqs([FromBody] FaqBulkRequest request)
        {
            var res = new AiResponse<string>();
            try
            {
                if (request?.Items == null || !request.Items.Any())
                {
                    res.Message = "No FAQ data provided.";
                    res.Status = false;
                    return res;
                }
                foreach (var faq in request.Items)
                {
                    AssignOptionIdsRecursively(faq.Options);
                }
                await _faqService.CreateManyAsync(request.Items);
                res.Message = "FAQs saved successfully";
                res.Status = true;
                res.Result = "FAQs saved successfully";
            }
            catch (Exception ex)
            {
                res.Message = "Error: " + ex.Message;
                res.Status = false;
            }
            return res;
        }
        //PUT (Update) FAQ
        [HttpPut("{id}")]
        public async Task<AiResponse<FaqItem>> PutFaqs(string id, [FromBody] FaqItem updatedFaq)
        {
            var res = new AiResponse<FaqItem>();
            try
            {
                var existingFaq = await _faqService.GetByIdAsync(id);
                if (existingFaq == null)
                {
                    res.Message = "FAQ not found";
                    res.Status = false;
                    return res;
                }
                AssignOptionIdsRecursively(updatedFaq.Options);
                updatedFaq.Id = id;
                await _faqService.UpdateAsync(id, updatedFaq);
                res.Message = "FAQ updated successfully";
                res.Status = true;
                res.Result = updatedFaq;
            }
            catch (Exception ex)
            {
                res.Message = "Error: " + ex.Message;
                res.Status = false;
            }
            return res;
        }
        //DELETE FAQ by ID
        [HttpDelete("{id}")]
        public async Task<AiResponse<string>> Delete(string id)
        {
            var res = new AiResponse<string>();
            try
            {
                var existingFaq = await _faqService.GetByIdAsync(id);
                if (existingFaq == null)
                {
                    res.Message = "FAQ not found";
                    res.Status = false;
                    return res;
                }
                await _faqService.DeleteAsync(id);
                res.Message = "FAQ deleted successfully";
                res.Status = true;
                res.Result = "FAQ deleted successfully";
            }
            catch (Exception ex)
            {
                res.Message = "Error: " + ex.Message;
                res.Status = false;
            }
            return res;
        }
        //DELETE All FAQs
        [HttpDelete("all")]
        public async Task<AiResponse<string>> DeleteAll()
        {
            var res = new AiResponse<string>();
            try
            {
                await _faqService.DeleteAllAsync();
                res.Message = "All FAQs deleted successfully";
                res.Status = true;
                res.Result = "All FAQs deleted successfully";
            }
            catch (Exception ex)
            {
                res.Message = "Error: " + ex.Message;
                res.Status = false;
            }
            return res;
        }
        // Chat-related endpoints(Start, session ID created)
        [HttpPost("chat/start")]
        public async Task<AiResponse<object>> StartChat([FromBody] ChatRequest request)
        {
            var res = new AiResponse<object>();
            try
            {
                string sessionId = Guid.NewGuid().ToString();
                var session = new ChatSession { SessionId = sessionId };
                session.Messages.Add(new ChatMessage
                {
                    Sender = "user",
                    Text = request.Message,
                    Timestamp = DateTime.UtcNow
                });
                var faqs = await _faqService.GetAsync();
                var matched = faqs.FirstOrDefault(f => f.Query.Equals(request.Message, StringComparison.OrdinalIgnoreCase));
                string reply;
                List<string> options = new List<string>();
                if (matched != null)
                {
                    var matchedOption = matched.Options?.FirstOrDefault(opt =>
                        opt.OptionText.Equals(request.Message, StringComparison.OrdinalIgnoreCase));
                    if (matchedOption != null)
                    {
                        reply = matchedOption.Response;
                    }
                    else
                    {
                        reply = matched.Response;
                        options = matched.Options?.Select(opt => opt.OptionText).ToList() ?? new List<string>();
                    }
                }
                else
                {
                    reply = "Sorry, I don't have an answer for that.";
                }
                session.Messages.Add(new ChatMessage
                {
                    Sender = "bot",
                    Text = reply,
                    Timestamp = DateTime.UtcNow
                });
                await _chatSessionService.CreateAsync(session);
                res.Message = "Chat started successfully";
                res.Status = true;
                res.Result = new { sessionId, reply, options };
            }
            catch (Exception ex)
            {
                res.Message = "Error: " + ex.Message;
                res.Status = false;
            }
            return res;
        }
        // Continue chat with session ID
        [HttpPost("chat/{sessionId}")]
        public async Task<AiResponse<object>> ContinueChat(string sessionId, [FromBody] ChatRequest request)
        {
            var res = new AiResponse<object>();
            try
            {
                var session = await _chatSessionService.GetBySessionIdAsync(sessionId);
                if (session == null)
                {
                    res.Message = "Session not found.";
                    res.Status = false;
                    return res;
                }

                session.Messages.Add(new ChatMessage
                {
                    Sender = "user",
                    Text = request.Message,
                    Timestamp = DateTime.UtcNow
                });

                var faqs = await _faqService.GetAsync();
                string reply;
                List<string> options = new List<string>();

                var matchResult = FindMatchRecursive(faqs, request.Message);
                if (matchResult != null)
                {
                    reply = matchResult.Value.reply;
                    options = matchResult.Value.options;
                }
                else
                {
                    reply = "Sorry, I don't have an answer for that.";
                }

                session.Messages.Add(new ChatMessage
                {
                    Sender = "bot",
                    Text = reply,
                    Timestamp = DateTime.UtcNow
                });

                await _chatSessionService.UpdateAsync(sessionId, session);

                res.Message = "Chat continued successfully";
                res.Status = true;
                res.Result = new { sessionId, reply, options };
            }
            catch (Exception ex)
            {
                res.Message = "Error: " + ex.Message;
                res.Status = false;
            }
            return res;
        }

        // Get chat history by session ID
        [HttpGet("chat/{sessionId}")]
        public async Task<AiResponse<ChatSession>> GetChatHistory(string sessionId)
        {
            var res = new AiResponse<ChatSession>();
            try
            {
                var session = await _chatSessionService.GetBySessionIdAsync(sessionId);
                if (session == null)
                {
                    res.Message = "Session not found.";
                    res.Status = false;
                }
                else
                {
                    res.Message = "Chat history fetched successfully";
                    res.Status = true;
                    res.Result = session;
                }
            }
            catch (Exception ex)
            {
                res.Message = "Error: " + ex.Message;
                res.Status = false;
            }
            return res;
        }
    }

    // Models for request and response
    public class ChatRequest
    {
        public string Message { get; set; }
    }
    // Generic response model
    public class AiResponse<T>
    {
        public string Message { get; set; } = string.Empty;
        public bool Status { get; set; } = false;
        public T? Result { get; set; }
    }
}
