﻿using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smartstore.Core.Content.Blocks
{
    public interface IBlockHtmlParts
    {
        void AddCssClass(string value);
        void AddHtmlAttribute(string name, object value);
    }

    /// <summary>
    /// Handles rendering of corresponding <see cref="IBlock"/> implementations.
    /// </summary>
    public interface IBlockHandler
    {
        /// <summary>
        /// Called when the block is about to be rendered. Gives you the chance to add css classes or html attributes to the root element.
        /// </summary>
        /// <param name="container">The block instance wrapper / model.</param>
        /// <param name="viewMode">The stories current view mode.</param>
        /// <param name="htmlParts">Allows adding classes or attributes.</param>
        void BeforeRender(IBlockContainer container, StoryViewMode viewMode, IBlockHtmlParts htmlParts);

        /// <summary>
        /// Renders the block result.
        /// </summary>
        /// <param name="element">The block element to render.</param>
        /// <param name="templates">A list of template names. The first valid template will be used for rendering.</param>
        /// <param name="htmlHeper">Html helper instance.</param>
        Task RenderAsync(IBlockContainer element, IEnumerable<string> templates, IHtmlHelper htmlHeper);

        /// <summary>
        /// Called after the entity has been saved to the database to perform operations which require an entity id (e.g. localization stuff).
        /// </summary>
        /// <param name="container">The block instance wrapper / model.</param>
        /// <param name="entity">
        /// The corresponding entity record for the block.
        /// </param>
        Task AfterSaveAsync(IBlockContainer container, IBlockEntity entity);

        /// <summary>
        /// Clones <see cref="IBlockEntity.Model"/>. In most cases it is sufficient to directly return the serialized model, but there may be cases
        /// where the inner data must be cloned first, e.g. if it contains a picture reference.
        /// </summary>
        /// <param name="sourceEntity">The source entity.</param>
        /// <param name="clonedEntity">The target entity clone which is about to be saved in the data storage.</param>
        /// <returns>The serialized block clone.</returns>
        Task<string> CloneAsync(IBlockEntity sourceEntity, IBlockEntity clonedEntity);

        /// <summary>
        /// returns the block's render result as string.
        /// </summary>
        /// <param name="element">The block element to render.</param>
        /// <param name="templates">A list of valid template names. The first valid template will be used for rendering.</param>
        /// <param name="htmlHeper">Html helper instance.</param>
        Task<IHtmlContent> ToHtmlContentAsync(IBlockContainer element, IEnumerable<string> templates, IHtmlHelper htmlHelper);
    }

    /// <summary>
    /// Handles loading, saving and rendering of corresponding <see cref="IBlock"/> implementations.
    /// </summary>
    public interface IBlockHandler<T> : IBlockHandler where T : IBlock
    {
        /// <summary>
        /// Creates the <see cref="IBlock"/> implementation instance for the passed block entity.
        /// </summary>
        /// <param name="entity">The serialized entity to create a block instance for.</param>
        /// <returns>A concrete block implementation instance.</returns>
        T Create(IBlockEntity entity);

        /// <summary>
        /// Creates and loads the <see cref="IBlock"/> implementation instance for the passed block entity and view mode.
        /// </summary>
        /// <param name="entity">The serialized entity to load a block instance for.</param>
        /// <param name="viewMode">Depending on view mode, some data may be loaded or not.</param>
        /// <returns>A concrete block implementation instance.</returns>
        Task<T> LoadAsync(IBlockEntity entity, StoryViewMode viewMode); // -------

        bool IsValid(T block);

        /// <summary>
        /// Serializes the passed block instance and saves the result in the <see cref="IBlockEntity.Model"/> property.
        /// </summary>
        /// <param name="block">The block instance to save.</param>
        /// <param name="entity">
        /// The corresponding entity record for the block. In most cases <paramref name="block"/> will
        /// be converted to JSON and assigned to <see cref="IBlockEntity.Model"/> property, which then will be saved in the data storage.
        /// </param>
        Task SaveAsync(T block, IBlockEntity entity);
    }
}
