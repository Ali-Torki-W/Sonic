export interface PagedResult<T> {
    items: readonly T[];
    page: number;
    pageSize: number;
    totalItems: number; // backend long
}
