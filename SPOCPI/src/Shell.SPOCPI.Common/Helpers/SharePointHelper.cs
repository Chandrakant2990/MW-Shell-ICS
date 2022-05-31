// <copyright file="SharePointHelper.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>

namespace Shell.SPOCPI.Common
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using Microsoft.Graph;

    /// <summary>
    /// The SharePointHelper class.
    /// </summary>
    public static class SharePointHelper
    {
        /// <summary>
        /// CreateSubscription is used to create a graph subscription for a drive i.e. library in SharePoint.
        /// </summary>
        /// <param name="client">The GraphServiceClient object.</param>
        /// <param name="subscription">The Subscription object.</param>
        /// <returns>The Graph Subscription object.</returns>
        public static async Task<Subscription> CreateSubscription(GraphServiceClient client, Subscription subscription)
        {
            if (client == null)
            {
                throw new System.ArgumentNullException(nameof(client), Resource.ClientNotNull);
            }

            if (subscription == null)
            {
                throw new System.ArgumentNullException(nameof(subscription), Resource.SubscriptionNotNull);
            }

            return await client.Subscriptions.Request().AddAsync(subscription).ConfigureAwait(true);
        }

        /// <summary>
        /// RenewSubscription is used to update the ExpirationDateTime for a graph subscription in case the subscription is going to expire.
        /// </summary>
        /// <param name="client">The GraphServiceClient object.</param>
        /// <param name="subscription">The Subscription object.</param>
        /// <returns>The Graph Subscription object.</returns>
        public static async Task<Subscription> RenewSubscription(GraphServiceClient client, Subscription subscription)
        {
            if (client == null)
            {
                throw new System.ArgumentNullException(nameof(client), Resource.ClientNotNull);
            }

            if (subscription == null)
            {
                throw new System.ArgumentNullException(nameof(subscription), Resource.SubscriptionNotNull);
            }

            var newSubscription = new Subscription
            {
                ExpirationDateTime = subscription.ExpirationDateTime.Value.UtcDateTime.AddDays(Constants.SubscriptionRenewDay),
            };

            return await client.Subscriptions[subscription.Id].Request().UpdateAsync(newSubscription).ConfigureAwait(true);
        }

        /// <summary>
        /// DeleteSubscription is used to delete the graph subscription.
        /// </summary>
        /// <param name="client">The GraphServiceClient object.</param>
        /// <param name="subscriptionId">The subscriptionId.</param>
        /// <returns>Returns a boolean value indicating whether the subscription is deleted or not.</returns>
        public static async Task<bool> DeleteSubscription(GraphServiceClient client, string subscriptionId)
        {
            if (client == null)
            {
                throw new System.ArgumentNullException(nameof(client), Resource.ClientNotNull);
            }

            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                throw new System.ArgumentNullException(nameof(subscriptionId), Resource.SubscriptionIdNotNull);
            }

            await client.Subscriptions[subscriptionId].Request().DeleteAsync().ConfigureAwait(true);

            return true;
        }

        /// <summary>
        /// GetSubscription is used to get the graph subscription based on the subscriptionId.
        /// </summary>
        /// <param name="client">The GraphServiceClient object.</param>
        /// <param name="subscriptionId">The subscription Id as string.</param>
        /// <returns>The Graph Subscription object.</returns>
        public static async Task<Subscription> GetSubscription(GraphServiceClient client, string subscriptionId)
        {
            if (client == null)
            {
                throw new System.ArgumentNullException(nameof(client), Resource.ClientNotNull);
            }

            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                throw new System.ArgumentNullException(nameof(subscriptionId), Resource.SubscriptionIdNotNull);
            }

            return await client.Subscriptions[subscriptionId].Request().GetAsync().ConfigureAwait(false);
        }

        [SuppressMessage("Microsoft.Design", "CA1054:UriPropertiesShouldNotBeStrings", Justification = "Reviewed.")]

        /// <summary>
        /// GetSite is used to get the Site object using Graph API.
        /// </summary>
        /// <param name="client">The GraphServiceClient object.</param>
        /// <param name="hostName">Host Name of the SharePoint web as string.</param>
        /// <param name="siteRelativeUrl">Site relative URL of the SharePoint web as string.</param>
        /// <returns>Return Site object.</returns>
        public static Site GetSite(GraphServiceClient client, string hostName, string siteRelativeUrl)
        {
            if (client == null)
            {
                throw new System.ArgumentNullException(nameof(client), Resource.ClientNotNull);
            }

            if (string.IsNullOrWhiteSpace(hostName))
            {
                throw new System.ArgumentNullException(nameof(hostName), Resource.HostNotNull);
            }

            if (string.IsNullOrWhiteSpace(siteRelativeUrl))
            {
                throw new System.ArgumentNullException(nameof(siteRelativeUrl), Resource.SiteRelativeUrlNotNull);
            }

            return client.Sites.GetByPath(siteRelativeUrl, hostName).Request().GetAsync().Result;
        }

        /// <summary>
        /// GetDrives is used to get the drives(document libraries) inside a SharePoint web.
        /// </summary>
        /// <param name="client">The GraphServiceClient object.</param>
        /// <param name="siteId">The graph site Id for a SharePoint web as string.</param>
        /// <returns>Collection of Drive objects.</returns>
        public static IList<Drive> GetDrives(GraphServiceClient client, string siteId)
        {
            IList<Drive> drives = new List<Drive>();

            if (client == null)
            {
                throw new System.ArgumentNullException(nameof(client), Resource.ClientNotNull);
            }

            if (string.IsNullOrWhiteSpace(siteId))
            {
                throw new System.ArgumentNullException(nameof(siteId), Resource.SiteIdNotNull);
            }

            ISiteDrivesCollectionPage libraries = client.Sites[siteId].Drives.Request().GetAsync().Result;
            while (libraries != null && libraries.CurrentPage != null && libraries.CurrentPage.Count > 0)
            {
                foreach (var item in libraries.CurrentPage)
                {
                    drives.Add(item);
                }

                libraries = libraries.NextPageRequest?.GetAsync().Result;
            }

            return drives;
        }

        /// <summary>
        /// Gets the list identifier.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="siteId">The site identifier.</param>
        /// <param name="driveId">The drive identifier.</param>
        /// <returns>The list id.</returns>
        public static string GetListId(GraphServiceClient client, string siteId, string driveId)
        {
            var lists = client.Sites[siteId].Drives[driveId].List.Request().GetAsync().Result;
            return lists?.Id;
        }

        /// <summary>
        /// GetLists is used to get the list in a SharePoint web.
        /// </summary>
        /// <param name="client">The GraphServiceClient object.</param>
        /// <param name="siteId">The graph site Id for a SharePoint web as string.</param>
        /// <returns>Collection of List objects.</returns>
        public static IList<List> GetLists(GraphServiceClient client, string siteId)
        {
            IList<List> listCollection = new List<List>();

            if (client is null)
            {
                throw new System.ArgumentNullException(nameof(client), Resource.ClientNotNull);
            }

            var lists = client.Sites[siteId].Lists.Request().GetAsync().Result;
            while (lists != null && lists.CurrentPage != null && lists.CurrentPage.Count > 0)
            {
                foreach (var item in lists.CurrentPage)
                {
                    listCollection.Add(item);
                }

                lists = lists.NextPageRequest?.GetAsync().Result;
            }

            return listCollection;
        }
    }
}
