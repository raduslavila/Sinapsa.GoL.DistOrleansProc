const API_BASE_URL = 'http://localhost:5050/api';

interface GridStateDto {
  width: number;
  height: number;
  cells: boolean[][];
}

export const initUniverse = async (
  chunksX: number,
  chunksY: number,
  chunkSize: number,
  liveDensity: number
): Promise<any> => {
  const response = await fetch(
    `${API_BASE_URL}/init?chunksX=${chunksX}&chunksY=${chunksY}&chunkSize=${chunkSize}&liveDensity=${liveDensity}`,
    { method: 'POST' }
  );

  if (!response.ok) {
    throw new Error('Failed to initialize universe');
  }

  return response.json();
};

export const reinitUniverse = async (
  chunksX: number,
  chunksY: number,
  chunkSize: number,
  liveDensity: number
): Promise<any> => {
  const response = await fetch(
    `${API_BASE_URL}/reinit?chunksX=${chunksX}&chunksY=${chunksY}&chunkSize=${chunkSize}&liveDensity=${liveDensity}`,
    { method: 'POST' }
  );

  if (!response.ok) {
    throw new Error('Failed to re-initialize universe');
  }

  return response.json();
};

export const runStep = async (): Promise<void> => {
  const response = await fetch(`${API_BASE_URL}/step`, { method: 'POST' });

  if (!response.ok) {
    throw new Error('Failed to run step');
  }
};

export const getGrid = async (): Promise<boolean[][]> => {
  const response = await fetch(`${API_BASE_URL}/grid`);

  if (!response.ok) {
    throw new Error('Failed to fetch grid');
  }

  const data: GridStateDto = await response.json();

  // Return the cells array directly
  return data.cells;
};
