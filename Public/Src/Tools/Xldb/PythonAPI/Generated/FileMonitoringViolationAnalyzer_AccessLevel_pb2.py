# -*- coding: utf-8 -*-
# Generated by the protocol buffer compiler.  DO NOT EDIT!
# source: FileMonitoringViolationAnalyzer_AccessLevel.proto

from google.protobuf.internal import enum_type_wrapper
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from google.protobuf import reflection as _reflection
from google.protobuf import symbol_database as _symbol_database
# @@protoc_insertion_point(imports)

_sym_db = _symbol_database.Default()




DESCRIPTOR = _descriptor.FileDescriptor(
  name='FileMonitoringViolationAnalyzer_AccessLevel.proto',
  package='BuildXL.Xldb.Proto',
  syntax='proto3',
  serialized_options=b'\252\002\022BuildXL.Xldb.Proto',
  serialized_pb=b'\n1FileMonitoringViolationAnalyzer_AccessLevel.proto\x12\x12\x42uildXL.Xldb.Proto*\xd7\x01\n+FileMonitoringViolationAnalyzer_AccessLevel\x12;\n7FileMonitoringViolationAnalyzer_AccessLevel_UNSPECIFIED\x10\x00\x12\x34\n0FileMonitoringViolationAnalyzer_AccessLevel_Read\x10\x01\x12\x35\n1FileMonitoringViolationAnalyzer_AccessLevel_Write\x10\x02\x42\x15\xaa\x02\x12\x42uildXL.Xldb.Protob\x06proto3'
)

_FILEMONITORINGVIOLATIONANALYZER_ACCESSLEVEL = _descriptor.EnumDescriptor(
  name='FileMonitoringViolationAnalyzer_AccessLevel',
  full_name='BuildXL.Xldb.Proto.FileMonitoringViolationAnalyzer_AccessLevel',
  filename=None,
  file=DESCRIPTOR,
  values=[
    _descriptor.EnumValueDescriptor(
      name='FileMonitoringViolationAnalyzer_AccessLevel_UNSPECIFIED', index=0, number=0,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='FileMonitoringViolationAnalyzer_AccessLevel_Read', index=1, number=1,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='FileMonitoringViolationAnalyzer_AccessLevel_Write', index=2, number=2,
      serialized_options=None,
      type=None),
  ],
  containing_type=None,
  serialized_options=None,
  serialized_start=74,
  serialized_end=289,
)
_sym_db.RegisterEnumDescriptor(_FILEMONITORINGVIOLATIONANALYZER_ACCESSLEVEL)

FileMonitoringViolationAnalyzer_AccessLevel = enum_type_wrapper.EnumTypeWrapper(_FILEMONITORINGVIOLATIONANALYZER_ACCESSLEVEL)
FileMonitoringViolationAnalyzer_AccessLevel_UNSPECIFIED = 0
FileMonitoringViolationAnalyzer_AccessLevel_Read = 1
FileMonitoringViolationAnalyzer_AccessLevel_Write = 2


DESCRIPTOR.enum_types_by_name['FileMonitoringViolationAnalyzer_AccessLevel'] = _FILEMONITORINGVIOLATIONANALYZER_ACCESSLEVEL
_sym_db.RegisterFileDescriptor(DESCRIPTOR)


DESCRIPTOR._options = None
# @@protoc_insertion_point(module_scope)
