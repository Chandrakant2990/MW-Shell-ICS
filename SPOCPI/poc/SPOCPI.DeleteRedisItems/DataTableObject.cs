namespace SPOCPI.DeleteRedisItems
{
    using Azure;
    using Azure.Search.Documents.Models;

    /// <summary>
    /// Data table object class.
    /// </summary>
    /// <typeparam name="T">Search Result type class.</typeparam>
    public class DataTableObject<T>
        where T : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataTableObject{T}"/> class.
        /// </summary>
        public DataTableObject()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTableObject{T}" /> class.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="draw">The draw.</param>
        /// <param name="recordsFiltered">The records filtered.</param>
        /// <param name="recordsTotal">The records total.</param>
        public DataTableObject(Pageable<SearchResult<T>> data, int? draw, int? recordsFiltered, int? recordsTotal)
        {
            this.Data = data;
            this.Draw = draw;
            this.RecordsFiltered = recordsFiltered;
            this.RecordsTotal = recordsTotal;
        }

        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <value>
        /// The data.
        /// </value>
        public Pageable<SearchResult<T>> Data { get; }

        /// <summary>
        /// Gets or sets the draw.
        /// </summary>
        /// <value>
        /// The draw.
        /// </value>
        public int? Draw { get; set; }

        /// <summary>
        /// Gets or sets the records filtered.
        /// </summary>
        /// <value>
        /// The records filtered.
        /// </value>
        public int? RecordsFiltered { get; set; }

        /// <summary>
        /// Gets or sets the records total.
        /// </summary>
        /// <value>
        /// The records total.
        /// </value>
        public int? RecordsTotal { get; set; }
    }
}
