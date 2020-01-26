import Generated
from Generated import *
import rocksdb
import os
import itertools
from itertools import *

class XldbDataStore:
    event_column_family_name = b"Event"
    pip_column_family_name = b"Pip"
    static_graph_column_family_name = b"StaticGraph"
    path_table_family_name = b"PathTable"
    inverse_path_table_family_name = b"InversePathTable"
    string_table_family_name = b"StringTable"
    inverse_string_table_family_name = b"InverseStringTable"

    xldb_version_file_name = "xldbversion.txt"

    default_worker_id = sys.maxsize
    default_file_rewrite_count = -1
    
    '''
    Constructor for the datastore
    @param path     The path to the rocksdb directory
    '''
    def __init__(self, path):
        column_families = {
            b'default': rocksdb.ColumnFamilyOptions(),
            pip_column_family_name : rocksdb.ColumnFamilyOptions(),
            event_column_family_name : rocksdb.ColumnFamilyOptions(),
            static_graph_column_family_name : rocksdb.ColumnFamilyOptions(),
            path_table_family_name : rocksdb.ColumnFamilyOptions(),
            string_table_family_name : rocksdb.ColumnFamilyOptions(),
            inverse_path_table_family_name : rocksdb.ColumnFamilyOptions(),
            inverse_string_table_family_name : rocksdb.ColumnFamilyOptions(),
        }

        class DynamicPrefix(rocksdb.interfaces.SliceTransform):
            def name(self):
                return b'dynamic'

            def transform(self, src):
                return (0, len(src))

            def in_domain(self, src):
                return True

            def in_range(self, dst):
                return True


        options = rocksdb.Options()
        options.prefix_extractor = DynamicPrefix()

        if not os.path.isdir(path):
            print("The path provided does not exist")
            sys.exit(1)

        self.db = rocksdb.DB(path, options, column_families=column_families)

        self.event_parser_dict = {}
        self.event_parser_dict[ExecutionEventId__pb2.FileArtifactContentDecided] = FileArtifactContentDecidedEvent()
        self.event_parser_dict[ExecutionEventId__pb2.WorkerList] = WorkerListEvent()
        self.event_parser_dict[ExecutionEventId__pb2.ExecutionEventId_PipExecutionPerformance] = PipExecutionPerformanceEvent()
        self.event_parser_dict[ExecutionEventId__pb2.DirectoryMembershipHashed] = DirectoryMembershipHashedEvent()
        self.event_parser_dict[ExecutionEventId__pb2.ProcessExecutionMonitoringReported] = ProcessExecutionMonitoringReportedEvent()
        self.event_parser_dict[ExecutionEventId__pb2.ProcessFingerprintComputation] = ProcessFingerprintComputationEvent()
        self.event_parser_dict[ExecutionEventId__pb2.ExecutionEventId_BuildSessionConfiguration] = BuildSessionConfigurationEvent()
        self.event_parser_dict[ExecutionEventId__pb2.DependencyViolationReported] = DependencyViolationReportedEvent()
        self.event_parser_dict[ExecutionEventId__pb2.PipCacheMiss] = PipCacheMissEvent()
        self.event_parser_dict[ExecutionEventId__pb2.ResourceUsageReported] = StatusReportedEvent()
        self.event_parser_dict[ExecutionEventId__pb2.BxlInvocation] = BxlInvocationEvent()
        self.event_parser_dict[ExecutionEventId__pb2.PipExecutionDirectoryOutputs] = PipExecutionDirectoryOutputsEvent()

        self.pip_parser_dict = {}
        self.pip_parser_dict[PipType_pb2.PipType_CopyFile] = CopyFile()
        self.pip_parser_dict[PipType_pb2.PipType_WriteFile] = WriteFile()
        self.pip_parser_dict[PipType_pb2.PipType_Process] = ProcessPip()
        self.pip_parser_dict[PipType_pb2.PipType_SealDirectory] = SealDirectory()
        self.pip_parser_dict[PipType_pb2.PipType_Ipc] = IpcPip()

    '''
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                                                                            Private Event Related APIs
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    '''

    '''
    Gets all events of a certain type from the DB
    '''
    def __get_events_by_type(self, event_type_id):

        event_key = EventKey()
        event_key.EventTypeId = event_type_id
        event_key.WorkerId = default_worker_id
        event_key.FileRewriteCount = default_file_rewrite_count

        return __get_events_by_key(event_key)

    '''
    Gets all events by an event key
    '''
    def __get_events_by_key(self, event_key):
        
        stored_events = []

        match_all_rewrite_counts = False
        match_all_worker_ids = False

        parser = self.event_parser_dict[event_key.EventTypeId]

        if event_key.FileRewriteCount == default_file_rewrite_count:
            match_all_rewrite_counts = True
            event_key.FileRewriteCount = 0
        
        if event_key.WorkerId == default_worker_id:
            match_all_worker_ids = True
            event_key.WorkerId = 0

        prefix = event_key.SerializeToString()
        
        it = self.db.get(self.db.get_column_family(event_column_family_name)).iteritems()
        it.seek(prefix)

        item = next(it, None)
        while item is not None:
            if (item[0].startsWith(prefix)):
                kvp_key = EventKey().ParseFromString(item[0])
                if match_all_worker_ids and match_all_rewrite_counts:
                    stored_events.append(parser.ParseFromString(item[1]))
                elif (match_all_worker_ids and kvp_key.FileRewriteCount == event_key.FileRewriteCount) or (match_all_rewrite_counts and kvp_key.WorkerId == event_key.WorkerId):
                    stored_events.append(parser.ParseFromString(item[1]))
                else:
                    stored_events.append(parser.ParseFromString(item[1]))
            else:
                break

        return stored_events
    
    '''
    Get events that only use the PipID as the key
    '''
    def get_events_by_pip_id_only(self, event_type_id, pip_id, worker_id=None):
        
        event_key = EventKey()
        event_key.EventTypeId = event_type_id
        if worker_id is None:
            event_key.WorkerId = default_worker_id
        else:
            event_key.WorkerId = worker_id
        event_key.FileRewriteCount = default_file_rewrite_count
        event_key.PipId = pip_id

        return __get_events_by_key(event_key)

    '''
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                                                                            Public Event Related APIs
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    '''

    '''
    Get DependencyViolatedEvents by key
    '''
    def get_dependency_violated_event_by_key(self, violater_pip_id, worker_id=None):
        
        event_key = EventKey()
        event_key.EventTypeId = ExecutionEventId_pb2.DependencyViolationReported
        if worker_id is None:
            event_key.WorkerId = default_worker_id
        else:
            event_key.WorkerId = worker_id
        event_key.FileRewriteCount = default_file_rewrite_count
        event_key.ViolaterPipId = violater_pip_id

        return __get_events_by_key(event_key)

    '''
    Get PipExecutionStepPerformance events by key
    '''
    def get_pip_execution_step_performance_by_key(self, pip_id, pip_execution_step=0, worker_id=None):
        
        event_key = EventKey()
        event_key.EventTypeId = ExecutionEventId_pb2.PipExecutionStepPerformanceReported
        if worker_id is None:
            event_key.WorkerId = default_worker_id
        else:
            event_key.WorkerId = worker_id
        event_key.FileRewriteCount = default_file_rewrite_count
        event_key.PipId = pip_id
        event_key.PipExecutionStepPerformanceKey = pip_execution_step

        return __get_events_by_key(event_key)

    '''
    Get ProcessFingerPrintComputationEvents by key
    '''
    def process_fingerprint_computation_events_by_key(self, pip_id, computation_kind=0, worker_id=None):

        event_key = EventKey()
        event_key.EventTypeId = ExecutionEventId_pb2.ProcessFingerprintComputation
        if worker_id is None:
            event_key.WorkerId = default_worker_id
        else:
            event_key.WorkerId = worker_id
        event_key.FileRewriteCount = default_file_rewrite_count
        event_key.PipId = pip_id
        event_key.ProcessFingerprintComputationKey = computation_kind

        return __get_events_by_key(event_key)

    '''
    Gets DirectoryMembershipHashedEvents by key
    '''
    def get_directory_membership_hashed_events_by_key(self, pip_id, directory_path="", worker_id=None):

        event_key = EventKey()
        event_key.EventTypeId = ExecutionEventId_pb2.DirectoryMembershipHashed
        if worker_id is None:
            event_key.WorkerId = default_worker_id
        else:
            event_key.WorkerId = worker_id
        event_key.FileRewriteCount = default_file_rewrite_count
        event_key.PipId = pip_id
        
        directory_id = 0
        if directory_path == "":
            possible_ids = get_ids_for_path(directory_path)
            if len(possible_ids) != 0:
                directory_id = possible_ids[0]
        
        event_key.DirectoryMembershipHashedKey = directory_id

        return __get_events_by_key(event_key)

    '''
    Gets PipExecutionDirectoryOuputEvents by key
    '''
    def get_pip_execution_directory_output_events_by_key(self, pip_id, directory_path="", worker_id=None):

        event_key = EventKey()
        event_key.EventTypeId = ExecutionEventId_pb2.PipExecutionDirectoryOutputs
        if worker_id is None:
            event_key.WorkerId = default_worker_id
        else:
            event_key.WorkerId = worker_id
        event_key.FileRewriteCount = default_file_rewrite_count
        event_key.PipId = pip_id
        
        directory_id = 0
        if directory_path == "":
            possible_ids = get_ids_for_path(directory_path)
            if len(possible_ids) != 0:
                directory_id = possible_ids[0]
        
        event_key.PipExecutionDirectoryOutputKey = directory_id

        return __get_events_by_key(event_key)
    
    '''
    Get FileArtifactContentDecidedEvent by key
    '''
    def get_file_artifact_content_decided_event_by_key(self, directory_path="", file_rewrite_count=None, worker_id=None):
        
        event_key = EventKey()
        event_key.EventTypeId = ExecutionEventId_pb2.FileArtifactContentDecided
        if worker_id is None:
            event_key.WorkerId = default_worker_id
        else:
            event_key.WorkerId = worker_id
        
        if file_rewrite_count is None:
            event_key.FileRewriteCount = default_file_rewrite_count
        else:
            event_key.FileRewriteCount = file_rewrite_count
        
        directory_id = 0
        if directory_path == "":
            possible_ids = get_ids_for_path(directory_path)
            if len(possible_ids) != 0:
                directory_id = possible_ids[0]
        
        event_key.FileArtifactContentDecidedKey = directory_id

        return __get_events_by_key(event_key)

    

    '''
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                                                                            String and Path Table APIs
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    '''

    '''
    Gets the string from an id
    '''
    def get_string_from_id(self, id):

        string_table_key = StringTableKey()
        string_table_key.id = id
        print()

        string_val = self.db.get(self.db.get_column_family(string_table_family_name), string_table_key.SerializeToString())

        return FullString().ParseFromString(string_val).value

    '''
    Gets the path from an id
    '''
    def get_path_from_id(self, id):

        path_table_key = PathTableKey()
        path_table_key.id = id
        print()

        string_val = self.db.get(self.db.get_column_family(path_table_family_name), path_table_key.SerializeToString())

        return AbsolutePath().ParseFromString(string_val).value

    '''
    Gets ids for a string
    '''
    def get_ids_for_string(self, string):

        string_id_list = []

        key = FullString()
        key.Value = string

        it = self.db.get(self.db.get_column_family(inverse_string_table_family_name)).iteritems()
        it.seek(prefix)

        item = next(it, None)
        while item is not None:
            if (item[0].startsWith(prefix)):
                string_id_list.append(StringTableKey().parseFromString(item[1]).Id)
            else:
                break

        return string_id_list

    '''
    Gets ids for a path
    '''
    def get_ids_for_path(self, path):

        path_id_list = []

        key = AbsolutePath()
        key.Value = path

        it = self.db.get(self.db.get_column_family(inverse_path_table_family_name)).iteritems()
        it.seek(prefix)

        item = next(it, None)
        while item is not None:
            if (item[0].startsWith(prefix)):
                path_id_list.append(PathTableKey().parseFromString(item[1]).Id)
            else:
                break

        return path_id_list


