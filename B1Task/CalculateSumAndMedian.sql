-- FUNCTION: public.calculate_sum_and_median()

-- DROP FUNCTION IF EXISTS public.calculate_sum_and_median();

CREATE OR REPLACE FUNCTION public.calculate_sum_and_median(
)
    RETURNS TABLE
            (
                sum_of_integers    bigint,
                median_of_decimals numeric
            )
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 1000

AS
$BODY$

BEGIN
    SELECT SUM(evennumber)
    INTO sum_of_integers
    FROM datatable;

    SELECT PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY floatingnumber)
    INTO median_of_decimals
    FROM datatable;

    RETURN QUERY SELECT sum_of_integers, median_of_decimals;
END;
$BODY$;

ALTER FUNCTION public.calculate_sum_and_median()
    OWNER TO postgres;
