using System;
using System.Diagnostics;
using System.Linq;
using Lucene.Net.Analysis;
using Raven.Client.Embedded;
using Raven.Client.Indexes;

namespace OverlappingDates
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var store = new EmbeddableDocumentStore {RunInMemory = true};

            store.Initialize();

            store
                .DatabaseCommands
                .PutIndex("MyDocuments",
                          new IndexDefinitionBuilder<MyDocument>
                              {
                                  Map = documents => from doc in documents
                                                     select new
                                                         {
                                                             doc.Start,
                                                             doc.End
                                                         },
                                  Analyzers =
                                      {
                                          {x => x.Start, "SimpleAnalyzer"},
                                          {x => x.End, "SimpleAnalyzer"}
                                      }
                              });

            using (var session = store.OpenSession())
            {
                var doc = new MyDocument
                    {
                        Start = DateTime.Parse("3 Dec 2013 12:00:00"),
                        End = DateTime.Parse("3 Dec 2013 13:00:00")
                    };
                session.Store(doc);
                session.SaveChanges();
            }
            
            var start = DateTime.Parse("3 Dec 2013 11:30:00");

            var end = DateTime.Parse("3 Dec 2013 12:30:00");


            // True AND True == True

            using (var session = store.OpenSession())
            {
                var results = session
                    .Advanced
                    .LuceneQuery<MyDocument>()
                    // !(3 Dec 2013 12:00:00 > 3 Dec 2013 12:30:00) == True
                    .Not.WhereGreaterThan("Start", end)
                    .AndAlso()
                    // !(3 Dec 2013 13:00:00 < 3 Dec 2013 11:30:00) == True
                    .Not.WhereLessThan("End", start)
                    .ToList();

                Debug.Assert(results.Count == 1);
            }
        }
    }
}