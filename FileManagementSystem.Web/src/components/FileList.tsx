import type { FileItemDto } from '../types';

interface FileListProps {
  files: FileItemDto[];
  isLoading: boolean;
  totalCount: number;
}

const FileList = ({ files, isLoading, totalCount }: FileListProps) => {
  const formatSize = (bytes: number) => {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(2) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(2) + ' MB';
  };

  if (isLoading) {
    return <div>Loading files...</div>;
  }

  return (
    <div>
      <h2>Files ({totalCount})</h2>
      {files.length === 0 ? (
        <p>No files found</p>
      ) : (
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr style={{ borderBottom: '2px solid #ccc' }}>
              <th style={{ textAlign: 'left', padding: '0.5rem' }}>Name</th>
              <th style={{ textAlign: 'right', padding: '0.5rem' }}>Size</th>
              <th style={{ textAlign: 'left', padding: '0.5rem' }}>Type</th>
              <th style={{ textAlign: 'left', padding: '0.5rem' }}>Tags</th>
              <th style={{ textAlign: 'left', padding: '0.5rem' }}>Date</th>
            </tr>
          </thead>
          <tbody>
            {files.map((file) => (
              <tr key={file.id} style={{ borderBottom: '1px solid #eee' }}>
                <td style={{ padding: '0.5rem' }}>{file.path.split('\\').pop() || file.path}</td>
                <td style={{ textAlign: 'right', padding: '0.5rem' }}>{formatSize(file.size)}</td>
                <td style={{ padding: '0.5rem' }}>{file.mimeType}</td>
                <td style={{ padding: '0.5rem' }}>{file.tags.join(', ') || '-'}</td>
                <td style={{ padding: '0.5rem' }}>{new Date(file.createdDate).toLocaleDateString()}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
};

export default FileList;
