using Xunit;
using FluentAssertions;
using LearningAssistant.Services.Learning;
using LearningAssistant.Models.Learning;
using LearningAssistant.Models.Learning.ValueObjects;
using LearningAssistant.Common;

namespace LearningAssistant.Tests
{
    public class LearningItemFormatterTests
    {
        [Fact]
        public void BuildFields_ChineseCharacter_ShouldReturnAllFields()
        {
            var item = new LearningItem
            {
                SubCategory = SubCategoryType.ChineseCharacter,
                MainContent = "霜",
                Pronunciation = Pronunciation.Create("shuāng"),
                Meaning = Meaning.Create("像雪一样的白色结晶"),
                CharacterFeatures = new CharacterFeatures { StrokeCount = "17", Radical = "雨", Structure = "上下结构" }
            };
            item.SetExtendedProperty("Words", "风霜、霜降、霜雪");

            var fields = LearningItemFormatter.BuildFields(item);

            fields.Should().HaveCount(6);
            fields.Select(f => f.Label).Should().Contain("拼音", "释义", "笔画", "部首", "结构", "组词");
            fields.First(f => f.Label == "拼音").SpeakText.Should().Be("shuāng");
            fields.First(f => f.Label == "拼音").IsPronunciation.Should().BeTrue();
        }

        [Fact]
        public void BuildFields_ChineseCharacter_EmptyValues_ShouldSkipEmptyFields()
        {
            var item = new LearningItem
            {
                SubCategory = SubCategoryType.ChineseCharacter,
                MainContent = "霜",
                Meaning = Meaning.Create("像雪一样的白色结晶")
            };

            var fields = LearningItemFormatter.BuildFields(item);

            fields.Should().HaveCount(1);
            fields.First().Label.Should().Be("释义");
        }

        [Fact]
        public void BuildFields_EnglishWord_ShouldReturnAllFields()
        {
            var item = new LearningItem
            {
                SubCategory = SubCategoryType.EnglishWord,
                MainContent = "apple",
                Pronunciation = Pronunciation.Create("/ˈæpl/", "/ˈæpl/", "/ˈæp.əl/"),
                Meaning = Meaning.Create("苹果"),
                WordFeatures = new WordFeatures 
                { 
                    PartOfSpeech = "n.", 
                    SyllableBreakdown = "ap-ple",
                    WordForms = "apples, appled, appling",
                    Collocations = "eat an apple, apple pie"
                },
                Example = Example.Create("I eat an apple every day.", "我每天吃一个苹果。")
            };

            var fields = LearningItemFormatter.BuildFields(item);

            fields.Should().HaveCount(10);
            fields.Select(f => f.Label).Should().Contain("词性", "音标", "英式", "美式", "拼读", "释义", "词形", "搭配", "例句", "例句翻译");
            fields.First(f => f.Label == "音标").SpeakText.Should().Be("/ˈæpl/");
            fields.First(f => f.Label == "音标").IsPronunciation.Should().BeTrue();
        }

        [Fact]
        public void BuildFields_EnglishWord_NoUkPhonetic_ShouldSkipUkPhonetic()
        {
            var item = new LearningItem
            {
                SubCategory = SubCategoryType.EnglishWord,
                MainContent = "apple",
                Pronunciation = Pronunciation.Create("/ˈæpl/", null, "/ˈæp.əl/"),
                Meaning = Meaning.Create("苹果")
            };

            var fields = LearningItemFormatter.BuildFields(item);

            fields.Select(f => f.Label).Should().NotContain("英式");
            fields.Select(f => f.Label).Should().Contain("美式");
        }

        [Fact]
        public void BuildFields_ChinesePhrase_ShouldReturnFields()
        {
            var item = new LearningItem
            {
                SubCategory = SubCategoryType.ChinesePhrase,
                MainContent = "学习",
                Pronunciation = Pronunciation.Create("xué xí"),
                Meaning = Meaning.Create("通过阅读、听讲、研究、实践等获得知识或技能"),
                Example = Example.Create("我每天都在学习新东西。")
            };

            var fields = LearningItemFormatter.BuildFields(item);

            fields.Should().HaveCount(3);
            fields.Select(f => f.Label).Should().Contain("拼音", "释义", "例句");
            fields.First(f => f.Label == "拼音").IsPronunciation.Should().BeTrue();
        }

        [Fact]
        public void BuildFields_ChineseIdiom_ShouldReturnFields()
        {
            var item = new LearningItem
            {
                SubCategory = SubCategoryType.ChineseIdiom,
                MainContent = "画蛇添足",
                Pronunciation = Pronunciation.Create("huà shé tiān zú"),
                Meaning = Meaning.Create("比喻做了多余的事情，反而不恰当"),
                Example = Example.Create("这个方案已经很好了，不要再画蛇添足了。")
            };

            var fields = LearningItemFormatter.BuildFields(item);

            fields.Should().HaveCount(3);
            fields.Select(f => f.Label).Should().Contain("拼音", "释义", "例句");
        }

        [Fact]
        public void BuildFields_ChinesePoem_ShouldReturnFields()
        {
            var item = new LearningItem
            {
                SubCategory = SubCategoryType.ChinesePoem,
                MainContent = "静夜思"
            };
            item.SetExtendedProperty("Author", "李白");
            item.SetExtendedProperty("Dynasty", "唐代");
            item.SetExtendedProperty("Content", "床前明月光，疑是地上霜。");

            var fields = LearningItemFormatter.BuildFields(item);

            fields.Should().HaveCount(3);
            fields.Select(f => f.Label).Should().Contain("作者", "朝代", "内容");
        }

        [Fact]
        public void BuildFields_EnglishPhrase_ShouldReturnFields()
        {
            var item = new LearningItem
            {
                SubCategory = SubCategoryType.EnglishPhrase,
                MainContent = "break time",
                Pronunciation = Pronunciation.Create("/breɪk taɪm/"),
                Meaning = Meaning.Create("休息时间"),
                Example = Example.Create("It's break time now.")
            };

            var fields = LearningItemFormatter.BuildFields(item);

            fields.Should().HaveCount(3);
            fields.Select(f => f.Label).Should().Contain("音标", "释义", "例句");
            fields.First(f => f.Label == "音标").IsPronunciation.Should().BeTrue();
        }

        [Fact]
        public void BuildFields_EnglishSentence_ShouldReturnFields()
        {
            var item = new LearningItem
            {
                SubCategory = SubCategoryType.EnglishSentence,
                MainContent = "I love learning.",
                Meaning = Meaning.Create("我热爱学习。")
            };

            var fields = LearningItemFormatter.BuildFields(item);

            fields.Should().HaveCount(1);
            fields.First().Label.Should().Be("翻译");
        }

        [Fact]
        public void BuildFields_MathFormula_ShouldReturnFields()
        {
            var item = new LearningItem
            {
                SubCategory = SubCategoryType.MathFormula,
                MainContent = "a² + b² = c²",
                Meaning = Meaning.Create("勾股定理"),
                Example = Example.Create("边长为3、4、5的三角形满足3²+4²=5²")
            };

            var fields = LearningItemFormatter.BuildFields(item);

            fields.Should().HaveCount(3);
            fields.Select(f => f.Label).Should().Contain("内容", "解释", "示例");
        }

        [Fact]
        public void BuildFields_PhysicsLaw_ShouldReturnFields()
        {
            var item = new LearningItem
            {
                SubCategory = SubCategoryType.PhysicsLaw,
                MainContent = "牛顿第二定律",
                Meaning = Meaning.Create("力等于质量乘以加速度"),
                Example = Example.Create("质量为2kg的物体受到10N的力，加速度为5m/s²")
            };
            item.SetExtendedProperty("Formula", "F = ma");

            var fields = LearningItemFormatter.BuildFields(item);

            fields.Should().HaveCount(4);
            fields.Select(f => f.Label).Should().Contain("内容", "解释", "公式", "示例");
        }

        [Fact]
        public void BuildFields_ChemistryEquation_ShouldReturnFields()
        {
            var item = new LearningItem
            {
                SubCategory = SubCategoryType.ChemistryEquation,
                MainContent = "水的电解",
                Meaning = Meaning.Create("水在通电条件下分解为氢气和氧气"),
                Example = Example.Create("电解18克水可以得到2克氢气和16克氧气")
            };
            item.SetExtendedProperty("Equation", "2H₂O → 2H₂ + O₂");

            var fields = LearningItemFormatter.BuildFields(item);

            fields.Should().HaveCount(4);
            fields.Select(f => f.Label).Should().Contain("内容", "解释", "方程式", "示例");
        }

        [Fact]
        public void BuildFields_HistoryEvent_ShouldReturnFields()
        {
            var item = new LearningItem
            {
                SubCategory = SubCategoryType.HistoryEvent,
                MainContent = "鸦片战争",
                Meaning = Meaning.Create("1840年英国对中国发动的侵略战争")
            };
            item.SetExtendedProperty("Time", "1840-1842年");
            item.SetExtendedProperty("Location", "中国");

            var fields = LearningItemFormatter.BuildFields(item);

            fields.Should().HaveCount(4);
            fields.Select(f => f.Label).Should().Contain("内容", "描述", "时间", "地点");
        }

        [Fact]
        public void BuildFields_GeographyKnowledge_ShouldReturnFields()
        {
            var item = new LearningItem
            {
                SubCategory = SubCategoryType.GeographyKnowledge,
                MainContent = "长江",
                Meaning = Meaning.Create("中国最长的河流"),
                Example = Example.Create("长江全长约6300公里")
            };
            item.SetExtendedProperty("Location", "中国");

            var fields = LearningItemFormatter.BuildFields(item);

            fields.Should().HaveCount(4);
            fields.Select(f => f.Label).Should().Contain("内容", "描述", "位置", "示例");
        }

        [Fact]
        public void BuildFields_BiologyConcept_ShouldReturnFields()
        {
            var item = new LearningItem
            {
                SubCategory = SubCategoryType.BiologyConcept,
                MainContent = "光合作用",
                Meaning = Meaning.Create("植物利用光能将二氧化碳和水转化为有机物"),
                Example = Example.Create("绿叶在光下制造淀粉")
            };
            item.SetExtendedProperty("Experiment", "普利斯特利实验");

            var fields = LearningItemFormatter.BuildFields(item);

            fields.Should().HaveCount(4);
            fields.Select(f => f.Label).Should().Contain("内容", "解释", "实验", "示例");
        }

        [Fact]
        public void BuildFields_UnknownSubCategory_ShouldReturnDefaultFields()
        {
            var item = new LearningItem
            {
                SubCategory = SubCategoryType.Unknown,
                MainContent = "未知内容",
                Meaning = Meaning.Create("未知释义")
            };

            var fields = LearningItemFormatter.BuildFields(item);

            fields.Should().HaveCount(2);
            fields.Select(f => f.Label).Should().Contain("内容", "释义");
        }

        [Fact]
        public void FormatDisplayText_ChineseCharacter_ShouldReturnFormattedString()
        {
            var item = new LearningItem
            {
                SubCategory = SubCategoryType.ChineseCharacter,
                MainContent = "霜",
                Pronunciation = Pronunciation.Create("shuāng"),
                Meaning = Meaning.Create("像雪一样的白色结晶")
            };

            var result = LearningItemFormatter.FormatDisplayText(item);

            result.Should().Contain("拼音: shuāng");
            result.Should().Contain("释义: 像雪一样的白色结晶");
        }

        [Fact]
        public void FormatDisplayStruct_ChineseCharacter_ShouldReturnStructString()
        {
            var item = new LearningItem
            {
                SubCategory = SubCategoryType.ChineseCharacter,
                MainContent = "霜",
                Pronunciation = Pronunciation.Create("shuāng"),
                Meaning = Meaning.Create("像雪一样的白色结晶")
            };

            var result = LearningItemFormatter.FormatDisplayStruct(item);

            result.Should().Contain("拼音:?");
            result.Should().Contain("释义:?");
        }

        [Fact]
        public void ContentField_HasSpeakText_ShouldReturnTrueWhenSpeakTextIsSet()
        {
            var fieldWithSpeakText = new ContentField("拼音", "shuāng", "shuāng");
            var fieldWithoutSpeakText = new ContentField("释义", "解释");

            fieldWithSpeakText.HasSpeakText.Should().BeTrue();
            fieldWithoutSpeakText.HasSpeakText.Should().BeFalse();
        }
    }
}