﻿// ----------------------------------------------------------------------------------
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

namespace Microsoft.WindowsAzure.Commands.ServiceManagement.HostedServices
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Management.Automation;
    using Commands.Utilities.Common;
    using Management.Compute;
    using Management.Compute.Models;
    using Management.Models;
    using Model;
    using WindowsAzure.ServiceManagement;
    using Properties;
    using Role = Management.Compute.Models.Role;

    [Cmdlet(VerbsCommon.Get, "AzureRole"), OutputType(typeof(RoleContext))]
    public class GetAzureRoleCommand : ServiceManagementBaseCmdlet
    {
        public GetAzureRoleCommand()
        {
        }

        public GetAzureRoleCommand(IServiceManagement channel)
        {
            Channel = channel;
        }

        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The name of the hosted service.")]
        public string ServiceName
        {
            get;
            set;
        }

        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "Deployment slot")]
        [ValidateSet(DeploymentSlotType.Staging, DeploymentSlotType.Production, IgnoreCase = true)]
        public string Slot
        {
            get;
            set;
        }

        [Parameter(Position = 2, Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "Name of the role.")]
        public string RoleName
        {
            get;
            set;
        }

        [Parameter(Position = 3, Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "Get Instance Details")]
        public SwitchParameter InstanceDetails
        {
            get;
            set;
        }

        public void GetRoleProcess()
        {
            OperationStatusResponse getDeploymentOperation;
            var currentDeployment = this.GetCurrentDeployment(out getDeploymentOperation);
            if (currentDeployment != null)
            {
                if (this.InstanceDetails.IsPresent == false)
                {
                    var roleContexts = new Collection<RoleContext>();
                    IList<Role> roles = null;
                    if (string.IsNullOrEmpty(this.RoleName))
                    {
                        roles = currentDeployment.Roles;
                    }
                    else
                    {
                        roles = new List<Role>(currentDeployment.Roles.Where(r => r.RoleName.Equals(this.RoleName, StringComparison.OrdinalIgnoreCase)));
                    }

                    foreach (var r in roles.Select(role => new RoleContext
                        {
                            InstanceCount = currentDeployment.Roles.Count(ri => ri.RoleName.Equals(role.RoleName, StringComparison.OrdinalIgnoreCase)), 
                            RoleName = role.RoleName, 
                            OperationDescription = this.CommandRuntime.ToString(), 
                            OperationStatus = getDeploymentOperation.Status.ToString(), 
                            OperationId = getDeploymentOperation.Id, 
                            ServiceName = this.ServiceName, 
                            DeploymentID = currentDeployment.PrivateId
                        }))
                    {
                        roleContexts.Add(r);
                    }

                    WriteObject(roleContexts, true);
                }
                else
                {
                    Collection<RoleInstanceContext> instanceContexts = new Collection<RoleInstanceContext>();
                    IList<Management.Compute.Models.RoleInstance> roleInstances = null;

                    if (string.IsNullOrEmpty(this.RoleName))
                    {
                        roleInstances = currentDeployment.RoleInstances;
                    }
                    else
                    {
                        roleInstances = new List<Management.Compute.Models.RoleInstance>(currentDeployment.RoleInstances.Where(r => r.RoleName.Equals(this.RoleName, StringComparison.OrdinalIgnoreCase)));
                    }

                    foreach (var role in roleInstances)
                    {
                        var context = new RoleInstanceContext()
                        {
                            ServiceName = this.ServiceName,
                            OperationId = getDeploymentOperation.Id,
                            OperationDescription = this.CommandRuntime.ToString(),
                            OperationStatus = getDeploymentOperation.Status.ToString(),
                            InstanceErrorCode = role.InstanceErrorCode,
                            //TODO: Talk to AzureSDK, type mismatch
//                            InstanceFaultDomain = role.InstanceFaultDomain,
                            InstanceName = role.InstanceName,
                            InstanceSize = role.InstanceSize,
                            InstanceStateDetails = role.InstanceStateDetails,
                            InstanceStatus = role.InstanceStatus,
                            //TODO: Talk to AzureSDK, type mismatch
//                            InstanceUpgradeDomain = role.InstanceUpgradeDomain,
                            RoleName = role.RoleName,
                            DeploymentID = currentDeployment.PrivateId,
                            //TODO: Old SM client library types are exposed to customer
                            //      We'll need to declare the copy of these types here not to break any customer code.
//                            InstanceEndpoints = role.EndInstanceEndpoints
                        };

                        instanceContexts.Add(context);
                    }

                    WriteObject(instanceContexts, true);
                }
            }
        }

        protected override void OnProcessRecord()
        {
            this.GetRoleProcess();
        }

        private DeploymentGetResponse GetCurrentDeployment(out OperationStatusResponse operation)
        {
            DeploymentSlot slot;
            if (!Enum.TryParse(this.Slot, out slot))
            {
                throw new ArgumentOutOfRangeException("Slot");
            }

            WriteVerboseWithTimestamp(Resources.GetDeploymentBeginOperation);
            DeploymentGetResponse deploymentGetResponse = this.ComputeClient.Deployments.GetBySlot(this.ServiceName, slot);
            operation = GetOperationNewSM(deploymentGetResponse.RequestId);
            WriteVerboseWithTimestamp(Resources.GetDeploymentCompletedOperation);

            return deploymentGetResponse;
        }
    }
}