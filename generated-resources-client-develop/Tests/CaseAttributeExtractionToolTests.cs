using CSEx.Json.Extensions;
using GeneratedResourceClient.GraphQl.GraphQlResultExtensions;
using Newtonsoft.Json;
using NUnit.Framework;
using Path = GeneratedResourceClient.GraphQl.GraphQlResultExtensions.Path;

namespace Tests
{
    public class CaseAttributeExtractionToolTests
    {
        private object _cases;

        [SetUp]
        public void SetUp()
        {
            _cases = GetCases();
        }

        [Test]
        public void GetAttributeByKey1()
        {
            var objects = _cases.GetItems("*.*.productionLiquidMassWellInterventionCurrent");
            Assert.That(objects.Count(), Is.EqualTo(1));
            Assert.That(objects.First(), Is.AssignableTo<IEnumerable<object>>());
        }

        [Test]
        public void GetAttributeByKey2()
        {
            var objects = _cases.GetItems("*.*.fEMModel");
            Assert.That(objects.Count(), Is.EqualTo(1));
            Assert.That(objects.First(), Is.InstanceOf<string>());
        }

        [Test]
        public void GetAttributeByKey3()
        {
            var obj = _cases.GetItem(new Path("cases.nodes.0"));
            var objects = obj.GetItems("oilUnitLosses");
            Assert.That(objects.Count(), Is.EqualTo(1));
        }

        [Test]
        public void GetAttributeByPath1()
        {
            var path = new Path("cases.nodes.productionLiquidMassWellInterventionCurrent");
            var objects = _cases.GetItems(path);
            Assert.That(objects.Count(), Is.EqualTo(1));
            Assert.That(objects.First(), Is.InstanceOf<IEnumerable<object>>());
        }

        [Test]
        public void GetAttributeByPath2()
        {
            var path = new Path("cases.nodes.productionLiquidMassWellInterventionCurrent.values.value");
            var objects = _cases.GetItems(path);
            Assert.That(objects.Count(), Is.EqualTo(1));
            Assert.That(objects.First(), Is.InstanceOf<double>());
        }

        [Test]
        public void GetAttributeByPath3()
        {
            var path = new Path("cases.nodes.fEMModel");
            var objects = _cases.GetItems(path);
            Assert.That(objects.Count(), Is.EqualTo(1));
            Assert.That(objects.First(), Is.InstanceOf<string>());
        }

        [Test]
        public void GetAttributeByPath4()
        {
            var path = new Path("cases.nodes.productionLiquidMass.values.2.value");
            var objects = _cases.GetItems(path);
            Assert.That(objects.Count(), Is.EqualTo(1));
            Assert.That(objects.First(), Is.EqualTo(2.9065319452054794));
        }

        [Test]
        public void GetAttributeByPath5()
        {
            var path = new Path("cases.nodes.productionLiquidMass.values.307.value");
            var objects = _cases.GetItems(path);
            Assert.That(objects.Count(), Is.EqualTo(0));
        }

        [Test]
        public void GetAttributeByPath6()
        {
            var path = new Path("cases.nodes.productionLiquidMass.values.value");
            var objects = _cases.GetItems(path);
            Assert.That(objects.Count(), Is.EqualTo(301));
        }

        [Test]
        public void GetAttributeByPath7()
        {
            var path = new Path("cases.nodes.0.*");
            var objects = _cases.GetItems(path);
            Assert.That(objects.Count(), Is.EqualTo(251));
        }

        [Test]
        public void GetAttributeByPath8()
        {
            var path = new Path("**.productionLiquidMassWellInterventionCurrent.*");
            var objects = _cases.GetItems(path);
            Assert.That(objects.Count(), Is.EqualTo(2));
        }

        [Test]
        public void GetAttributeByPath9()
        {
            var path = new Path("**.productionLiquidMassWellInterventionCurrent.**.*");
            var objects = _cases.GetItems(path);
            Assert.That(objects.Count(), Is.EqualTo(2));
        }

        [Test]
        public void GetAttributeByPath10()
        {
            var path = new Path("");
            var objects = _cases.GetItems(path);
            Assert.That(objects.Count(), Is.EqualTo(1));
        }

        [Test]
        public void GetAttributeByWildcard1()
        {
            var path = new Path("*.*.globalId");
            var objects = _cases.GetItems(path);
            Assert.That(objects.FirstOrDefault(), Is.EqualTo("038b4a40-1639-4c17-a430-db1cc1416faa"));
        }

        [Test]
        public void GetAttributeByWildcard2()
        {
            var path = new Path("**.nameShortRu");
            var objects = _cases.GetItems(path);
            Assert.That(objects.Count(), Is.EqualTo(1));
            Assert.That(objects.FirstOrDefault(), Is.EqualTo("2021_Зарезки_Суторминское_факт"));
        }

        [Test]
        public void GetAttributeByWildcard3()
        {
            var path = new Path("cases.**.nameShortRu");
            var objects = _cases.GetItems(path);
            Assert.That(objects.Count(), Is.EqualTo(1));
            Assert.That(objects.FirstOrDefault(), Is.EqualTo("2021_Зарезки_Суторминское_факт"));
        }

        [Test]
        public void GetAttributeByWildcard4()
        {
            var path = new Path("cases.nodes.**.nameShortRu");
            var objects = _cases.GetItems(path);
            Assert.That(objects.Count(), Is.EqualTo(1));
            Assert.That(objects.FirstOrDefault(), Is.EqualTo("2021_Зарезки_Суторминское_факт"));
        }

        [Test]
        public void GetValuesByWildcard1()
        {
            var path = new Path("**.value");
            var objects = _cases.GetItems(path);
            Assert.That(objects.Count(), Is.EqualTo(17852));
        }

        [Test]
        public void GetValuesByWildcard2()
        {
            var path = new Path("**.productionLiquidMassWellInterventionCurrent.values.value");
            var objects = _cases.GetItems(path);
            Assert.That(objects.Count(), Is.EqualTo(1));
        }

        [Test]
        public void GetValuesByWildcard3()
        {
             var path = new Path("**.productionLiquidMassWellInterventionCurrent.**.value");
            var objects = _cases.GetItems(path);
            Assert.That(objects.Count(), Is.EqualTo(1));
        }

        private object GetCases()
        {
            var str = File.ReadAllText("cases.json");
            var _cases = JsonConvert.DeserializeObject<IDictionary<string, object>>(str, new JsonSerializerSettings
            {
                Converters = new List<JsonConverter>
                {
                    new JsonDictionaryConverter()
                }
            })!;

            return _cases;
        }
    }
}
