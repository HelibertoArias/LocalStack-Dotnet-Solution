using Amazon.Runtime;
using Amazon.S3;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ConsoleApp
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            await RunS3Example();
        }

        private static async Task RunS3Example()
        {
            await using (var stacklocal = new LocalStackContainer())
            {
                await stacklocal.RunContainer();

                var config = new AmazonS3Config()
                {
                    ServiceURL = "http://localhost:4572/",
                    ForcePathStyle = true,
                    Timeout = TimeSpan.FromSeconds(5)
                };
                var credentials = new BasicAWSCredentials("tem", "temp");

                IAmazonS3 s3Client = new AmazonS3Client(credentials, config);

                var bucketName = "bucket-test";

                Console.WriteLine($"Creating bucket {bucketName}");
                await s3Client.PutBucketAsync(bucketName);

                var list = await s3Client.ListBucketsAsync();

                Console.WriteLine("Reading buckets...");
                list.Buckets.ForEach((bucket) => { Console.WriteLine(bucket.BucketName); });

                const string path = @"dummy-file.jpg";

                var file = File.Create(path);

                Console.WriteLine("Uploading a file..");
                await s3Client.UploadObjectFromStreamAsync(bucketName, path, file, null);

                Console.WriteLine($"List files in bucket {bucketName}");
                var filesResponse = await s3Client.ListObjectsAsync(bucketName);
                filesResponse.S3Objects.ForEach((s3Object) => Console.WriteLine(s3Object.Key));
            }

            Console.ReadLine();
        }
    }
}