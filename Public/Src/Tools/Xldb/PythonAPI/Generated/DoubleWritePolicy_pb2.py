# -*- coding: utf-8 -*-
# Generated by the protocol buffer compiler.  DO NOT EDIT!
# source: DoubleWritePolicy.proto

from google.protobuf.internal import enum_type_wrapper
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from google.protobuf import reflection as _reflection
from google.protobuf import symbol_database as _symbol_database
# @@protoc_insertion_point(imports)

_sym_db = _symbol_database.Default()




DESCRIPTOR = _descriptor.FileDescriptor(
  name='DoubleWritePolicy.proto',
  package='BuildXL.Xldb.Proto',
  syntax='proto3',
  serialized_options=b'\252\002\022BuildXL.Xldb.Proto',
  serialized_pb=b'\n\x17\x44oubleWritePolicy.proto\x12\x12\x42uildXL.Xldb.Proto*\x93\x01\n\x11\x44oubleWritePolicy\x12!\n\x1d\x44oubleWritePolicy_UNSPECIFIED\x10\x00\x12\x19\n\x15\x44oubleWritesAreErrors\x10\x01\x12 \n\x1c\x41llowSameContentDoubleWrites\x10\x02\x12\x1e\n\x1aUnsafeFirstDoubleWriteWins\x10\x03\x42\x15\xaa\x02\x12\x42uildXL.Xldb.Protob\x06proto3'
)

_DOUBLEWRITEPOLICY = _descriptor.EnumDescriptor(
  name='DoubleWritePolicy',
  full_name='BuildXL.Xldb.Proto.DoubleWritePolicy',
  filename=None,
  file=DESCRIPTOR,
  values=[
    _descriptor.EnumValueDescriptor(
      name='DoubleWritePolicy_UNSPECIFIED', index=0, number=0,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='DoubleWritesAreErrors', index=1, number=1,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='AllowSameContentDoubleWrites', index=2, number=2,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='UnsafeFirstDoubleWriteWins', index=3, number=3,
      serialized_options=None,
      type=None),
  ],
  containing_type=None,
  serialized_options=None,
  serialized_start=48,
  serialized_end=195,
)
_sym_db.RegisterEnumDescriptor(_DOUBLEWRITEPOLICY)

DoubleWritePolicy = enum_type_wrapper.EnumTypeWrapper(_DOUBLEWRITEPOLICY)
DoubleWritePolicy_UNSPECIFIED = 0
DoubleWritesAreErrors = 1
AllowSameContentDoubleWrites = 2
UnsafeFirstDoubleWriteWins = 3


DESCRIPTOR.enum_types_by_name['DoubleWritePolicy'] = _DOUBLEWRITEPOLICY
_sym_db.RegisterFileDescriptor(DESCRIPTOR)


DESCRIPTOR._options = None
# @@protoc_insertion_point(module_scope)
