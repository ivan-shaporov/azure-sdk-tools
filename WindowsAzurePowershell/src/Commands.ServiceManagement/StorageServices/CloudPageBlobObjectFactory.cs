// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------


namespace Microsoft.WindowsAzure.Commands.ServiceManagement.StorageServices
{
    using System;
    using Storage.Blob;
    using Storage.RetryPolicies;
    using Sync.Download;
    using Sync.Upload;
    using WindowsAzure.ServiceManagement;

    public class CloudPageBlobObjectFactory : ICloudPageBlobObjectFactory
    {
        private readonly TimeSpan delayBetweenRetries = TimeSpan.FromSeconds(10);
        private readonly StorageCredentialsFactory credentialsFactory;
        private TimeSpan operationTimeout;


        public CloudPageBlobObjectFactory(StorageCredentialsFactory credentialsFactory, TimeSpan operationTimeout)
        {
            this.credentialsFactory = credentialsFactory;
            this.operationTimeout = operationTimeout;
        }

        public CloudPageBlob Create(BlobUri destination)
        {
            return new CloudPageBlob(new Uri(destination.BlobPath), credentialsFactory.Create(destination));
        }

        public bool CreateContainer(BlobUri destination)
        {
            if (String.IsNullOrEmpty(destination.Uri.Query))
            {
                var destinationBlob = Create(destination);
                return destinationBlob.Container.CreateIfNotExists(this.CreateRequestOptions());
            }
            return true;
        }

        public BlobRequestOptions CreateRequestOptions()
        {
            return new BlobRequestOptions
                       {
                           ServerTimeout = this.operationTimeout,
                           RetryPolicy = new LinearRetry(delayBetweenRetries, 5)
                       };
        }
    }
}