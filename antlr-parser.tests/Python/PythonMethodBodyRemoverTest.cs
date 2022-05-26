using System;
using System.Collections.Generic;
using antlr_parser.Antlr4Impl;
using antlr_parser.Antlr4Impl.Python;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using Xunit;

namespace antlr_parser.tests.Python
{
    public class PythonMethodBodyRemoverTest
    {
        [Fact]
        public void KeepLastLine()
        {
            string source = @"
                def f():
                    should_be_removed()
                    return 10
            ".TrimIndent2();

            List<Tuple<int, int>> blocksToRemove = PythonMethodBodyRemover.FindBlocksToRemove(source);

            var methodBodyRemovalResult = MethodBodyRemovalResult.From(source, blocksToRemove);

            methodBodyRemovalResult.ShortenedSource.Should().Be(@"
                def f():
                    return 10
            ".TrimIndent2());
        }

        [Fact]
        public void KeepLastLine2()
        {
            string source = @"
                def f():
                    should_be_removed()
                    return 10


                def g():
                    should_be_removed()
                    return 10
            ".TrimIndent2();

            List<Tuple<int, int>> blocksToRemove = PythonMethodBodyRemover.FindBlocksToRemove(source);

            var methodBodyRemovalResult = MethodBodyRemovalResult.From(source, blocksToRemove);

            methodBodyRemovalResult.ShortenedSource.Should().Be(@"
                def f():
                    return 10


                def g():
                    return 10
            ".TrimIndent2());
        }

        [Fact]
        public void SmokeTest()
        {
            string source = @"
                def get_test_tfdataset(self, test_dataset: tf.data.Dataset) -> tf.data.Data_set:
                    """"""comment should be kept""""""
                    should_be_kept()

                def f():
                    should_be_kept()

                def get_test_tfdataset(self, test_dataset: tf.data.Dataset):
                    should_be_kept()

                def get_eval_tfdataset(self, eval_dataset: Optional[tf.data.Dataset] = None) -> tf.data.Dataset:
                    should_be_removed()
                    should_be_kept()

                def __init__(self, 
                    compute_metrics: Optional[Callable[[EvalPrediction], Dict]] = None, 
                    optimizers: Tuple[tf.keras.optimizers.Optimizer, tf.keras.optimizers.schedules.LearningRateSchedule] = (None, None,)
                ):
                    should_be_removed()
                    should_be_kept()
            ".TrimIndent2();

            List<Tuple<int, int>> blocksToRemove = PythonMethodBodyRemover.FindBlocksToRemove(source);

            var methodBodyRemovalResult = MethodBodyRemovalResult.From(source, blocksToRemove);

            methodBodyRemovalResult.ShortenedSource.Should().Be(@"
                def get_test_tfdataset(self, test_dataset: tf.data.Dataset) -> tf.data.Data_set:
                    """"""comment should be kept""""""
                    should_be_kept()

                def f():
                    should_be_kept()

                def get_test_tfdataset(self, test_dataset: tf.data.Dataset):
                    should_be_kept()

                def get_eval_tfdataset(self, eval_dataset: Optional[tf.data.Dataset] = None) -> tf.data.Dataset:
                    should_be_kept()

                def __init__(self, 
                    compute_metrics: Optional[Callable[[EvalPrediction], Dict]] = None, 
                    optimizers: Tuple[tf.keras.optimizers.Optimizer, tf.keras.optimizers.schedules.LearningRateSchedule] = (None, None,)
                ):
                    should_be_kept()
            ".TrimIndent2());
        }

        [Fact]
        public void SmokeTest2()
        {
            string source = @"
                import inspect
                import itertools as it


                def digest_config(obj, kwargs, caller_locals={}):
                    """"""
                    Sets init args and CONFIG values as local variables

                    The purpose of this function is to ensure that all
                    configuration of any object is inheritable, able to
                    be easily passed into instantiation, and is attached
                    as an attribute of the object.
                    """"""

                    # Assemble list of CONFIGs from all super classes
                    classes_in_hierarchy = [obj.__class__]
                    static_configs = []
                    while len(classes_in_hierarchy) > 0:
                        Class = classes_in_hierarchy.pop()
                        classes_in_hierarchy += Class.__bases__
                        if hasattr(Class, ""CONFIG""):
                            static_configs.append(Class.CONFIG)

                    # Order matters a lot here, first dicts have higher priority
                    caller_locals = filtered_locals(caller_locals)
                    all_dicts = [kwargs, caller_locals, obj.__dict__]
                    all_dicts += static_configs
                    obj.__dict__ = merge_dicts_recursively(*reversed(all_dicts))


                def merge_dicts_recursively(*dicts):
                    """"""
                    Creates a dict whose keyset is the union of all the
                    input dictionaries.  The value for each key is based
                    on the first dict in the list with that key.

                    dicts later in the list have higher priority

                    When values are dictionaries, it is applied recursively
                    """"""
                    result = dict()
                    all_items = it.chain(*[d.items() for d in dicts])
                    for key, value in all_items:
                        if key in result and isinstance(result[key], dict) and isinstance(value, dict):
                            result[key] = merge_dicts_recursively(result[key], value)
                        else:
                            result[key] = value
                    return result

            ".TrimIndent2();

            List<Tuple<int, int>> blocksToRemove = PythonMethodBodyRemover.FindBlocksToRemove(source);

            var methodBodyRemovalResult = MethodBodyRemovalResult.From(source, blocksToRemove);

            methodBodyRemovalResult.ShortenedSource.Should().Be(@"
                import inspect
                import itertools as it


                def digest_config(obj, kwargs, caller_locals={}):
                    """"""
                    Sets init args and CONFIG values as local variables

                    The purpose of this function is to ensure that all
                    configuration of any object is inheritable, able to
                    be easily passed into instantiation, and is attached
                    as an attribute of the object.
                    """"""
                    obj.__dict__ = merge_dicts_recursively(*reversed(all_dicts))


                def merge_dicts_recursively(*dicts):
                    """"""
                    Creates a dict whose keyset is the union of all the
                    input dictionaries.  The value for each key is based
                    on the first dict in the list with that key.

                    dicts later in the list have higher priority

                    When values are dictionaries, it is applied recursively
                    """"""
                    return result

            ".TrimIndent2());
        }

        [Fact]
        public void ConsistentParenthesis()
        {
            string source = @"
                def partial_bezier_points(
                    points: Sequence[np.ndarray],
                    a: float,
                    b: float
                ) -> list[float]:
                    """"""
                    Given an list of points which define
                    """"""
                    if a == 1:
                        return [points[-1]] * len(points)

                    a_to_1 = [
                        bezier(points[i:])(a)
                        for i in range(len(points))
                    ]
                    end_prop = (b - a) / (1. - a)
                    return [
                        bezier(a_to_1[:i + 1])(end_prop)
                        for i in range(len(points))
                    ]
            ".TrimIndent2();
            
            List<Tuple<int, int>> blocksToRemove = PythonMethodBodyRemover.FindBlocksToRemove(source);

            var methodBodyRemovalResult = MethodBodyRemovalResult.From(source, blocksToRemove);

            methodBodyRemovalResult.ShortenedSource.Should().Be(@"
                def partial_bezier_points(
                    points: Sequence[np.ndarray],
                    a: float,
                    b: float
                ) -> list[float]:
                    """"""
                    Given an list of points which define
                    """"""
                    return [
                        bezier(a_to_1[:i + 1])(end_prop)
                        for i in range(len(points))
                    ]
            ".TrimIndent2());
        }
    }
}