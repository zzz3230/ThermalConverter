using System.Collections;
using CSharpCustomExtensions.Collections.Dictionary;
using GeneratedResourceClient.Graph;
using GeneratedResourceClient.GraphMaster.Tools;
using GeneratedResourceClient.Opm;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Nntc.ObjectModel;

namespace Tests
{
    public class Tests
    {
        private GeneratedResourcesConvertor _convertor;

        [SetUp]
        public void Setup()
        {
            var loader = new OpmTypesDownloader(new Uri("http://100.111.0.51:7126/api/metadata"), new NullLogger<OpmTypesDownloader>());
            var metadata = loader.GetMetadata().Result;
            _convertor = new GeneratedResourcesConvertor(new MemoryCache(new MemoryCacheOptions()), new MemoryCache(new MemoryCacheOptions()));
        }

        private IEnumerable<Record> GetCollection()
        {
            for (int i = 0; i < 100; i++)
            {
                yield return new Record(Guid.NewGuid(), "TestType2", Guid.NewGuid().ToString(), "B");
            }
        }

        [Test]
        public void Test1()
        {
            var item = new
            {
                Type = "Value",
                Comment = "Comment",
                Descriotion = "Description",
                Name = "Name",
                Test = "Test",
                Dict = new Dictionary<string, object>()
                {
                    {"A", 3},
                    {"AB", "Aaq"},
                    {"Id", Guid.NewGuid()}
                },
                Left = new
                {
                    Id = Guid.NewGuid(),
                    Type = "A",
                },
                Right = new
                {
                    Id = Guid.NewGuid(),
                    Type = "A",
                },
                GroupLeft = new
                {
                    Type = "GroupType",
                    Name = "Gr",
                    Id = Guid.Empty,
                    Items = GetCollection().Select(x => new { Type = "ItemType", Id = x.id, b = x.B }),
                    Items2 = new List<CollectionItem>() { new CollectionItem() { Type = "GceItemType", Name = "GCE", Id = Guid.NewGuid() } }
                },
                GroupRight = new
                {
                    Navigation = "GroupTypeSecond",
                    Type = "GroupType",
                    Name = "Gr2",
                    Id = Guid.NewGuid()
                }
            };

            var test = _convertor.Convert(item);
            ;
            var res = test.ToTypedCollection((from, to, sourceItem, targetItem, property) =>
            {
                var path = (sourceItem.GetOrDefault("Navigation")?.ToString() ?? from, targetItem.GetOrDefault("Navigation")?.ToString() ?? to);

                if (from.Equals("ItemType"))
                {
                    return new TypeInfo((Multiplicity.Many, Multiplicity.Many), RelationshipDirection.Forward, path);
                }

                return new TypeInfo((Multiplicity.One, Multiplicity.One), RelationshipDirection.Forward, (from, to));
            });

            var typedCol = res.GeneratedResourcesCollection;
            var val = typedCol["Value"].First();
            Assert.IsTrue(Guid.TryParse(val["id"].ToString(), out _));
            Assert.IsTrue(typedCol.Count == 5);
            //Assert.That(res.Relations[("ItemType", "GroupType")].Count, Is.EqualTo(100));
            //Assert.That(res.Relations.Count, Is.EqualTo(1));

            //Assert.That(typedCol["GceItemType"][0]["GroupTypeId"], Is.EqualTo(Guid.Empty));
        }

        [Test]
        public void TestNoGrEntityAttribute()
        {
            try
            {
                var item = new Item()
                {
                    Name = "Name",
                    id = Guid.NewGuid(),
                    TestNullValue = null,
                    SubItem = new Item() { id = Guid.NewGuid(), Name = "SubItem", Type = "SubItem" }
                };

                var test = _convertor.Convert(item, removeNullOrEmptyValues: false);
                var testVal = test.TypedGroups;

                Assert.That(testVal["Item"][0]["TestNullValue"], Is.Null);

                var tg = test.ConvertTest();

                Assert.That(tg.Count, Is.EqualTo(1));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [Test]
        public void TestFailIfNull()
        {
            try
            {
                _ = _convertor.Convert(null!);
                Assert.Fail("Пропущен null");
            }
            catch (ArgumentNullException e)
            {
                Assert.Pass();
            }
            catch (Exception ex)
            {
                Assert.Fail();
            }
        }

        [Test]
        public void TestConvertSingleItem()
        {
            try
            {
                var item = new Item() { Name = "Name", id = Guid.NewGuid(), AddableSubItem = new Item() { id = Guid.NewGuid(), Name = "SubItem", Type = "SubItem" } };

                var test = _convertor.Convert(item);
                var converted = test.ConvertTest()["Item"].First();

                Assert.IsTrue(converted["type"] is "Item", "type mst be is Item");
                Assert.IsTrue(converted["Age"] is 32, "Age must be is 32");
                Assert.IsTrue(Guid.TryParse(converted["id"].ToString(), out _), "id Must be Guid");
                //var sub = test["SubItem"].First();

                //Assert.IsTrue(sub["ItemId"].Equals(item.id), "Не проставилась сылка на родительский элемент");
                //Assert.IsTrue(sub["id"].Equals(item.AddableSubItem.id), "Не проставился Id дочернего элемента");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        [Test]
        public void TestConvertDictionary()
        {
            try
            {
                var item = new Dictionary<string, object>()
                {
                    { "Id", "123" },
                    { "Name", "NameValue" }
                };

                var test = _convertor.Convert(item);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        [Test]
        public void TestClassWrapperWithRecordCollection()
        {
            var item = new GroupWithAttribute() { Name = "Gr", Id = Guid.Empty, Items = GetCollection() };

            var test = _convertor.Convert(item);
            var tg = test.ConvertTest();

            Assert.IsTrue(tg.ContainsKey("GroupWithAttribute"));
            Assert.IsTrue(tg.ContainsKey("TestType2"));
            Assert.IsTrue(tg.ContainsKey("CollectionItem"));

            Assert.IsTrue(tg.Count == 3);
        }

        [Test]
        public void TestClassWrapperWithAnonimusCollection()
        {
            var item = new Group() { Name = "Gr", Id = Guid.Empty, Items = GetCollection().Select(x => new { type = "anonimusType", Id = x.id, b = x.B }) };

            var test = _convertor.Convert(item);
            var tg = test.ConvertTest();

            Assert.IsTrue(tg.ContainsKey("Group"));
            Assert.IsTrue(tg.ContainsKey("CollectionItem"));
            Assert.IsTrue(tg.Count == 2);
        }

        [Test]
        public void TestAnonimusWrapperWithAnonimusCollection()
        {
            var item = new { type = "Wrapper", Name = "Gr", Id = Guid.Empty, Items = GetCollection().Select(x => new { type = "anonimusType", Id = x.id, b = x.B }) };

            var test = _convertor.Convert(item);
            var tg = test.ConvertTest();

            Assert.IsTrue(tg.ContainsKey("Wrapper"));
            Assert.IsTrue(tg.ContainsKey("anonimusType"));
            Assert.IsTrue(tg.Count == 2);

        }

        [Test]
        public void TestAnonimusWrapwerWithRecordTypeCollection()
        {
            var item = new { type = "CoreType", Name = "Gr", Id = Guid.Empty, Items = GetCollection() };

            var test = _convertor.Convert(item);
            var tg = test.ConvertTest();

            Assert.IsTrue(tg.ContainsKey("CoreType"));
            Assert.IsTrue(tg.ContainsKey("TestType2"));
            Assert.IsTrue(tg.Count == 2);
        }
    }

    public record Record(Guid id, string type, string A, string B);

    public class Item
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public int Age { get; set; } = 32;
        public string Descriotion { get; set; }
        [NotGrEntity]
        public Item SubItem { get; set; }
        public Item AddableSubItem { get; set; }

        public Guid id { get; set; }

        public IEnumerable Collection { get; set; } = new List<object>() { new { A = "A" } };
        public Optional<Guid> Optional { get; set; } = Guid.NewGuid();
        public bool? TestNullValue { get; set; }
    }

    public class Optional<T>
    {
        public Optional(T value)
        {
            Value = value;
        }

        public bool isempty => Value != null;

        private T Value { get; set; }

        public static implicit operator Optional<T>(T optional)
        {
            return new Optional<T>(optional);
        }

        public static explicit operator T(Optional<T> optional)
        {
            return optional.Value;
        }
    }
    class CollectionItem
    {
        public string Type { get; set; }
        public Guid Id { get; set; }
        public string Name { get; set; } = "CollectionItemName";

    }
    class GroupWithAttribute
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        [GrEntity]
        public IEnumerable<object> Items { get; set; }
        public List<CollectionItem>? Items2 { get; set; } = new List<CollectionItem>() { new CollectionItem() { Name = "Name" } };
    }
    class Group
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public IEnumerable<object> Items { get; set; }
        public List<CollectionItem>? Items2 { get; set; } = new List<CollectionItem>() { new CollectionItem() { Name = "Name" } };
    }
}