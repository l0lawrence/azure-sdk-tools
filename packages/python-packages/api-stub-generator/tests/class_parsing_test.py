# -------------------------------------------------------------------------
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License. See License.txt in the project root for
# license information.
# --------------------------------------------------------------------------

from apistub.nodes import FunctionNode, ClassNode, VariableNode, KeyNode
from apistubgentest.models import (
    FakeInventoryItemDataClass as FakeInventoryItemDataClass,
    FakeTypedDict as FakeTypedDict,
    FakeObject as FakeObject,
    PublicPrivateClass as PublicPrivateClass
)


class TestClassParsing:
    
    def _check_nodes(self, nodes, checks):
        assert len(nodes) == len(checks)
        for (i, node) in enumerate(nodes):
            (check_class, check_name, check_type) = checks[i]
            assert isinstance(node, check_class)
            actual_name = node.name
            assert actual_name == check_name
            if not check_class == FunctionNode:
                actual_type = node.type
                assert actual_type == check_type

    def test_data_class(self):
        class_node = ClassNode("test", None, FakeInventoryItemDataClass, "test")
        self._check_nodes(class_node.child_nodes, [
            (VariableNode, "name", "str"),
            (VariableNode, "quantity_on_hand", "int"),
            (VariableNode, "unit_price", "float"),
            (FunctionNode, "total_cost", None)
        ])

    def test_typed_dict_class(self):
        class_node = ClassNode("test", None, FakeTypedDict, "test")
        self._check_nodes(class_node.child_nodes, [
            (KeyNode, '"age"', "int"),
            (KeyNode, '"name"', "str"),
            (KeyNode, '"union"', "Union[bool, tests.class_parsing_test.FakeObject, PetEnum]")
        ])

    def test_object(self):
        class_node = ClassNode("test", None, FakeObject, "test")
        self._check_nodes(class_node.child_nodes, [
            (VariableNode, "age", "int"),
            (VariableNode, "name", "str"),
            (VariableNode, "union", "Union[bool, PetEnum]"),
            (FunctionNode, "__init__", None)
        ])

    def test_public_private(self):
        class_node = ClassNode("test", None, PublicPrivateClass, self.pkg_namespace)
        self._check_nodes(class_node.child_nodes, [
            (VariableNode, "public_dict", "dict"),
            (VariableNode, "public_var", "str"),
            (FunctionNode, "__init__", None),
            (FunctionNode, "public_func", None)
        ])
        class_node = ClassNode("test", None, PublicPrivateClass, "test")
        self._check_nodes(class_node.child_nodes, [
            (VariableNode, "public_dict", "{'a': 'b'}"),
            (VariableNode, "public_var", "str"),
            (FunctionNode, "__init__", None),
            (FunctionNode, "public_func", None)
        ])
