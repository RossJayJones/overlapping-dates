using System;
using System.Linq;
using Raven.Client;
using Raven.Client.Embedded;
using Raven.Client.Indexes;

namespace OverlappingDates
{
    internal class Program
    {
        private static IDocumentStore CreateStore()
        {
            var store = new EmbeddableDocumentStore { RunInMemory = true };

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
                                                 }
                          });

            return store;
        }

        private static void Populate(IDocumentStore store)
        {
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
        }

        private static void Main(string[] args)
        {
            var store = CreateStore();

            Populate(store);
            
            var start = DateTime.Parse("3 Dec 2013 11:30:00");

            var end = DateTime.Parse("3 Dec 2013 12:30:00");

            using (var session = store.OpenSession())
            {
                session.Advanced.AllowNonAuthoritativeInformation = false;

                var results = session
                    .Query<MyDocument>("MyDocuments")
                    .Where(x => x.Start < end && x.End > start)
                    .ToList();

                Console.WriteLine("Results: " + results.Count);
            }

            Console.ReadLine();
        }
    }
}