# -*- coding: utf-8 -*-
# Generated by the protocol buffer compiler.  DO NOT EDIT!
# source: ProducerConsumerType.proto

from google.protobuf.internal import enum_type_wrapper
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from google.protobuf import reflection as _reflection
from google.protobuf import symbol_database as _symbol_database
# @@protoc_insertion_point(imports)

_sym_db = _symbol_database.Default()




DESCRIPTOR = _descriptor.FileDescriptor(
  name='ProducerConsumerType.proto',
  package='BuildXL.Xldb.Proto',
  syntax='proto3',
  serialized_options=b'\252\002\022BuildXL.Xldb.Proto',
  serialized_pb=b'\n\x1aProducerConsumerType.proto\x12\x12\x42uildXL.Xldb.Proto*\x82\x01\n\x14ProducerConsumerType\x12$\n ProducerConsumerType_UNSPECIFIED\x10\x00\x12!\n\x1dProducerConsumerType_Producer\x10\x01\x12!\n\x1dProducerConsumerType_Consumer\x10\x02\x42\x15\xaa\x02\x12\x42uildXL.Xldb.Protob\x06proto3'
)

_PRODUCERCONSUMERTYPE = _descriptor.EnumDescriptor(
  name='ProducerConsumerType',
  full_name='BuildXL.Xldb.Proto.ProducerConsumerType',
  filename=None,
  file=DESCRIPTOR,
  values=[
    _descriptor.EnumValueDescriptor(
      name='ProducerConsumerType_UNSPECIFIED', index=0, number=0,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='ProducerConsumerType_Producer', index=1, number=1,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='ProducerConsumerType_Consumer', index=2, number=2,
      serialized_options=None,
      type=None),
  ],
  containing_type=None,
  serialized_options=None,
  serialized_start=51,
  serialized_end=181,
)
_sym_db.RegisterEnumDescriptor(_PRODUCERCONSUMERTYPE)

ProducerConsumerType = enum_type_wrapper.EnumTypeWrapper(_PRODUCERCONSUMERTYPE)
ProducerConsumerType_UNSPECIFIED = 0
ProducerConsumerType_Producer = 1
ProducerConsumerType_Consumer = 2


DESCRIPTOR.enum_types_by_name['ProducerConsumerType'] = _PRODUCERCONSUMERTYPE
_sym_db.RegisterFileDescriptor(DESCRIPTOR)


DESCRIPTOR._options = None
# @@protoc_insertion_point(module_scope)
