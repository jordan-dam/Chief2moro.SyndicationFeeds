﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using Chief2moro.SyndicationFeeds.Models;
using EPiServer;
using EPiServer.Core;
using EPiServer.Web;
using EPiServer.Web.Routing;

namespace Chief2moro.SyndicationFeeds
{
    public class SyndicationItemFactory
    {
        protected IContentLoader ContentLoader;
        protected IFeedContentResolver FeedContentResolver;
        protected SyndicationFeedPageType FeedPage;
      
        public SyndicationItemFactory(IContentLoader contentLoader, IFeedContentResolver feedContentResolver, SyndicationFeedPageType feedPage)
        {
            ContentLoader = contentLoader;
            FeedContentResolver = feedContentResolver;
            FeedPage = feedPage;
        }

        /// <summary>
        /// Gets a list of populated syndication items created from the dependent content references on the gived SyndicationFeedPage.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SyndicationItem> GetSyndicationItems()
        {
            var dependentContentItems = FeedContentResolver.GetContentReferences(FeedPage);
            var syndicationItems = dependentContentItems.Select(CreateItemFromReference).ToList();

            return syndicationItems.OrderByDescending(c => c.LastUpdatedTime).Take(FeedPage.MaximumItems);
        }

        private SyndicationItem CreateItemFromReference(ContentReference contentReference)
        {
            var content = ContentLoader.Get<IContent>(contentReference);
            return CreateSyndicationItem(content);
        }

        private SyndicationItem CreateSyndicationItem(IContent content)
        {
            var changeTrackable = content as IChangeTrackable;
            var changed = DateTime.Now;
            var changedby = string.Empty;

            if (changeTrackable != null)
            {
                changed = changeTrackable.Saved;
                changedby = changeTrackable.ChangedBy;
            }
            
            var item = new SyndicationItem
            {
                Id = content.ContentLink.ID.ToString(),
                Title = new TextSyndicationContent(content.Name),
                Summary = new TextSyndicationContent(FeedInformationHandler.SetItemDescription(content)),
                PublishDate = changed,
                LastUpdatedTime = changed,
            };
            
            var mimeType = GetMimeType(content);
            Uri url = GetItemUrl(content);

            item.Content = new UrlSyndicationContent(url, mimeType);
            item.AddPermalink(url);
            item.Authors.Add(new SyndicationPerson(string.Empty, changedby, string.Empty));

            return item;
        }

        private string GetMimeType(IContent content)
        {
            if (!(content is MediaData)) 
                return "text/html";
            
            var mediaContent = content as MediaData;
            return mediaContent.MimeType;
        }

        private Uri GetItemUrl(IContent content)
        {
            var feedPageUrl = UrlResolver.Current.GetUrl(FeedPage.ContentLink);

            string contentUrl = content is BlockData
                ? string.Format("{0}item?contentId={1}", feedPageUrl, content.ContentLink.ID)
                : UrlResolver.Current.GetUrl(content.ContentLink);

            var absoluteSiteUrl = SiteDefinition.Current.SiteUrl.ToString().TrimEnd('/');
            return new Uri(absoluteSiteUrl + contentUrl);
        }
    }
}