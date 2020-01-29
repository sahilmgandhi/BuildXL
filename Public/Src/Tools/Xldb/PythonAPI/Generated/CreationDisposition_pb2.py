# -*- coding: utf-8 -*-
# Generated by the protocol buffer compiler.  DO NOT EDIT!
# source: CreationDisposition.proto

from google.protobuf.internal import enum_type_wrapper
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from google.protobuf import reflection as _reflection
from google.protobuf import symbol_database as _symbol_database
# @@protoc_insertion_point(imports)

_sym_db = _symbol_database.Default()




DESCRIPTOR = _descriptor.FileDescriptor(
  name='CreationDisposition.proto',
  package='BuildXL.Xldb.Proto',
  syntax='proto3',
  serialized_options=b'\252\002\022BuildXL.Xldb.Proto',
  serialized_pb=b'\n\x19\x43reationDisposition.proto\x12\x12\x42uildXL.Xldb.Proto*\x98\x01\n\x13\x43reationDisposition\x12#\n\x1f\x43reationDisposition_UNSPECIFIED\x10\x00\x12\x0e\n\nCREATE_NEW\x10\x01\x12\x11\n\rCREATE_ALWAYS\x10\x02\x12\x11\n\rOPEN_EXISTING\x10\x03\x12\x0f\n\x0bOPEN_ALWAYS\x10\x04\x12\x15\n\x11TRUNCATE_EXISTING\x10\x05\x42\x15\xaa\x02\x12\x42uildXL.Xldb.Protob\x06proto3'
)

_CREATIONDISPOSITION = _descriptor.EnumDescriptor(
  name='CreationDisposition',
  full_name='BuildXL.Xldb.Proto.CreationDisposition',
  filename=None,
  file=DESCRIPTOR,
  values=[
    _descriptor.EnumValueDescriptor(
      name='CreationDisposition_UNSPECIFIED', index=0, number=0,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='CREATE_NEW', index=1, number=1,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='CREATE_ALWAYS', index=2, number=2,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='OPEN_EXISTING', index=3, number=3,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='OPEN_ALWAYS', index=4, number=4,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='TRUNCATE_EXISTING', index=5, number=5,
      serialized_options=None,
      type=None),
  ],
  containing_type=None,
  serialized_options=None,
  serialized_start=50,
  serialized_end=202,
)
_sym_db.RegisterEnumDescriptor(_CREATIONDISPOSITION)

CreationDisposition = enum_type_wrapper.EnumTypeWrapper(_CREATIONDISPOSITION)
CreationDisposition_UNSPECIFIED = 0
CREATE_NEW = 1
CREATE_ALWAYS = 2
OPEN_EXISTING = 3
OPEN_ALWAYS = 4
TRUNCATE_EXISTING = 5


DESCRIPTOR.enum_types_by_name['CreationDisposition'] = _CREATIONDISPOSITION
_sym_db.RegisterFileDescriptor(DESCRIPTOR)


DESCRIPTOR._options = None
# @@protoc_insertion_point(module_scope)