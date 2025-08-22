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
    [Tags("FAQ Management")]
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

    }

    // Generic response model
    public class AiResponse<T>
    {
        public string Message { get; set; } = string.Empty;
        public bool Status { get; set; } = false;
        public T? Result { get; set; }
    }
}
