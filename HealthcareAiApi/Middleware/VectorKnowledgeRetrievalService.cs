using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.AI;

namespace HealthcareAiApi.Middleware
{
    public class VectorKnowledgeRetrievalService
    {
        private readonly string _knowledgeBasePath;
        private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;

        public VectorKnowledgeRetrievalService(IWebHostEnvironment env, IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
        {
            _knowledgeBasePath = Path.Combine(env.ContentRootPath, "KnowledgeBase");
            _embeddingGenerator = embeddingGenerator;
        }

        public async Task<string> SearchRelevantContextAsync(string userQuery)
        {
            if (!Directory.Exists(_knowledgeBasePath)) return string.Empty;

            var policyFiles = Directory.GetFiles(_knowledgeBasePath, "*.txt");
            var allChunks = new List<string>();

            foreach (var file in policyFiles)
            {
                var text = await File.ReadAllTextAsync(file);
                // Split policy blocks by headers or policy IDs
                var chunks = text.Split(new[] { "[Policy ID:" }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(c => "[Policy ID:" + c.Trim());
                allChunks.AddRange(chunks);
            }

            if (allChunks.Count == 0) return "No internal guidelines found.";

            // Generate embedding for the incoming user query
            var queryEmbedding = await _embeddingGenerator.GenerateAsync(new[] { userQuery });
            
            string bestMatch = allChunks.First();
            double highestSimilarity = -1;

            // Compare query embedding against each policy chunk via Cosine Similarity
            foreach (var chunk in allChunks)
            {
                var chunkEmbedding = await _embeddingGenerator.GenerateAsync(new[] { chunk });
                double similarity = CosineSimilarity(queryEmbedding[0].Vector.ToArray(), chunkEmbedding[0].Vector.ToArray());
                
                if (similarity > highestSimilarity)
                {
                    highestSimilarity = similarity;
                    bestMatch = chunk;
                }
            }

            return bestMatch;
        }

        private static double CosineSimilarity(float[] vectorA, float[] vectorB)
        {
            double dotProduct = 0, normA = 0, normB = 0;
            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                normA += Math.Pow(vectorA[i], 2);
                normB += Math.Pow(vectorB[i], 2);
            }
            return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
        }
    }
}